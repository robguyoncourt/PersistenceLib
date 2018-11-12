using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence
{
	public class PersistSQLServer : IPersistDestination, IDisposable
	{
		private const string DB_CONNECTION_STRING = "Server={0};Database={1};User Id={2};Password={3};";
		private const int DB_COMMAND_TIMEOUT = 30;

		private static readonly Object _lockObj = new Object();

		public readonly string DBCONKEY_SERVER = "SERVER";
		public readonly string DBCONKEY_DATABASE = "DATABASE";
		public readonly string DBCONKEY_USERID = "USERID";
		public readonly string DBCONKEY_PASSWORD = "PASSWORD";

		private readonly List<(string key, string name)> _persistenceTableNames;
		private readonly string _dbSchemaName;
		private readonly SqlConnection _dbConnection;
		private readonly Dictionary<string, ParameterisedSQL> _parameterisedQueryCache;

		private bool _disposed;
		private Dictionary<string, TableInfo> _persistenceSchema;

		public PersistSQLServer(string dbSchemaName, List<(string key, string name)> persistenceTableNames)
		{
			_dbConnection = new SqlConnection();
			_dbSchemaName = dbSchemaName;
			_persistenceTableNames = persistenceTableNames;
			_parameterisedQueryCache = new Dictionary<string, ParameterisedSQL>();

		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				if (_dbConnection != null)
				{
					if (this.IsConnected)
						_dbConnection.Close();

					_dbConnection.Dispose();
				}
			}

			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool IsConnected => _dbConnection.State == ConnectionState.Executing || 
									_dbConnection.State == ConnectionState.Fetching || 
									_dbConnection.State == ConnectionState.Open;

		public void Connect(Dictionary<string, string> connectionParams)
		{
			_dbConnection.ConnectionString = @"Data Source=DESKTOP-EP61E82;Initial Catalog=Persistence;Integrated Security=False;User ID=ADOUSER;Password=latentzero";
				//string.Format(DB_CONNECTION_STRING, connectionParams[DBCONKEY_SERVER], connectionParams[DBCONKEY_DATABASE],
				//								connectionParams[DBCONKEY_USERID], connectionParams[DBCONKEY_PASSWORD]);
			_dbConnection.Open();

			_persistenceSchema = _persistenceTableNames.ToDictionary(tn => tn.name, tn => new TableInfo(_dbConnection, _dbSchemaName, tn.name));

		}

		public void Disconnect()
		{
			_dbConnection.Close();
		}

		public Task Persist(TransactionElements tes)
		{
			lock (_lockObj)
			{
				using (SqlTransaction sqlTransaction = _dbConnection.BeginTransaction())
				{
					try
					{
						foreach (var element in tes.Elements)
						{
							SqlCommand sqlCommand = BuildSQLCommandForElement(element);
							sqlCommand.Connection = _dbConnection;
							sqlCommand.CommandTimeout = DB_COMMAND_TIMEOUT;
							sqlCommand.Transaction = sqlTransaction;
							sqlCommand.ExecuteNonQuery();
						}
						sqlTransaction.Commit();
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						throw ex;
					}
				}
			}

			return Task.CompletedTask;
		}

		private SqlCommand BuildSQLCommandForElement(TransactionElement element)
		{

			ParameterisedSQL paramSQL;

			if (!_parameterisedQueryCache.TryGetValue(element.GetDBStatementKey(), out paramSQL))
			{
				paramSQL = CreateParameterSQLAndAddToCache(element, paramSQL);
			}

			SqlCommand newCommand = new SqlCommand();
			newCommand.CommandText = paramSQL.SQL;
			foreach (var col in paramSQL.Cols)
			{
				newCommand.Parameters.Add(new SqlParameter("@"+col.Name, col.Type, col.Length));
				newCommand.Parameters["@" + col.Name].Value = element.Values[col.Name]; // assumption that xml attributes and columns have same names
			}

			return newCommand;
		}

		private ParameterisedSQL CreateParameterSQLAndAddToCache(TransactionElement element, ParameterisedSQL paramSQL)
		{
			TableInfo table = _persistenceSchema[element.ElementName];
			StringBuilder parameterBuilder = new StringBuilder();
			StringBuilder valueBuilder = new StringBuilder();
			List<TableInfo.ColumnInfo> includedCols = new List<TableInfo.ColumnInfo>();

			switch (element.Action)
			{
				case ActionType.Insert:

					//Build insert values
					foreach (var col in element.Values.Keys)
					{
						if (table.ColumnDefinitions.ContainsKey(col)) // assumption that xml attributes and columns have same names 
						{
							includedCols.Add(table.ColumnDefinitions[col]);
							parameterBuilder.Append('@');
							parameterBuilder.Append(col + ",");
							valueBuilder.Append(col + ",");
						}
					}
					string insertSQL = $"INSERT INTO {table.Name} ({valueBuilder.ToString().TrimEnd(',')}) VALUES ({parameterBuilder.ToString().TrimEnd(',')})";
					paramSQL = new ParameterisedSQL(insertSQL, includedCols);
					_parameterisedQueryCache.Add(element.GetDBStatementKey(), paramSQL);

					break;

				case ActionType.Update:

					//Build update values
					foreach (var col in element.Values.Keys)
					{
						if (table.ColumnDefinitions.ContainsKey(col)) // assumption that xml attributes and columns have same names 
						{
							includedCols.Add(table.ColumnDefinitions[col]);
							if (col != element.ElementKey)
							{
								valueBuilder.Append(col);
								parameterBuilder.Append(col);
								parameterBuilder.Append(" = @");
								parameterBuilder.Append(col);
								parameterBuilder.Append(",");
							}
						}
					}
					string amendSQL = $"UPDATE {table.Name} SET {parameterBuilder.ToString().TrimEnd(',')} WHERE {element.ElementKey} = @{element.ElementKey}";
					paramSQL = new ParameterisedSQL(amendSQL, includedCols);
					_parameterisedQueryCache.Add(element.GetDBStatementKey(), paramSQL);

					break;
			}

			return paramSQL;
		}

		internal class ParameterisedSQL
		{
			internal string SQL { get; private set; }
			internal List<TableInfo.ColumnInfo> Cols { get; private set; }

			internal ParameterisedSQL(string sql, List<TableInfo.ColumnInfo> cols)
			{
				SQL = sql;
				Cols = cols;
			}
		}

		internal class TableInfo
		{
			internal string Schema { get; private set; }
			internal string Name { get; private set; }
			internal Dictionary<string, ColumnInfo> ColumnDefinitions { get; private set; } = new Dictionary<string, ColumnInfo>();
			internal TableInfo(SqlConnection dbConnection, string schema, string name)
			{
				Schema = schema;
				Name = name.ToLower();
				PopulateColumnDefinitions(dbConnection);
			}

			private void PopulateColumnDefinitions(SqlConnection dbConnection)
			{
				DataTable tableSchema = GetSchemaTable(dbConnection);

				foreach (DataRow row in tableSchema.Rows)
				{
					string colName = row["ColumnName"].ToString().ToLower();
					ColumnDefinitions.Add(colName, new ColumnInfo(colName, (SqlDbType)(int)row["ProviderType"], (int)row["ColumnSize"]));
				}
			}

			private DataTable GetSchemaTable(SqlConnection dbConnection)
			{
				SqlCommand cmd = dbConnection.CreateCommand();
				cmd.CommandText = "SET FMTONLY ON; SELECT * FROM " + Name +" ; SET FMTONLY OFF";
				using (SqlDataReader rdr = cmd.ExecuteReader())
				{
					return rdr.GetSchemaTable();
				}
			}

			internal class ColumnInfo
			{
				internal string Name { get; private set; }
				internal SqlDbType Type { get; private set; }
				internal int Length { get; private set; }
				internal ColumnInfo(string name, SqlDbType type, int length)
				{
					Name = name.ToLower();
					Type = type;
					Length = length;
				}

			}

		}
	}


}
