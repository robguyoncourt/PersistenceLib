using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence
{
	public class PersistenceDB : IPersistDestination, IDisposable
	{
		private const int DB_COMMAND_TIMEOUT = 30;

		private static readonly Object _lockObj = new Object();

		private readonly List<(string key, string name)> _persistenceTableNames;
		private readonly string _dbSchemaName;
		private readonly IDBConnection _dbConnection;
		private readonly Dictionary<string, ParameterisedSQL> _parameterisedQueryCache;

		private bool _disposed;
		private Dictionary<string, TableInfo> _persistenceSchema;

		public PersistenceDB(string dbSchemaName, List<(string key, string name)> persistenceTableNames, IDBConnection dbConnection)
		{
			_dbConnection = dbConnection;
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
					if (_dbConnection.IsConnected)
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

		public void Connect(Dictionary<string, string> connectionParams)
		{
			_dbConnection.Open(connectionParams);

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
				using (IDBTransaction sqlTransaction = _dbConnection.BeginTransaction())
				{
					try
					{
						foreach (var element in tes.Elements)
						{
							ParameterisedSQL parameterisedSql = GetParameterisedSQLForElement(element);
							Dictionary<string, IDBSQLParameter> sqlParmas = PopulateSQLParamsWithElementValues(element, parameterisedSql);
							sqlTransaction.ExecuteNonQuery(parameterisedSql.SQL, sqlParmas.Values.ToList(), DB_COMMAND_TIMEOUT);
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

		private Dictionary<string, IDBSQLParameter> PopulateSQLParamsWithElementValues(TransactionElement element, ParameterisedSQL parameterisedSql)
		{
			var sqlParmas = parameterisedSql.Params;
			foreach (var col in parameterisedSql.Cols)
			{
				sqlParmas[col.SQLParamName].Value = GetDBValue(element, col);
			}

			return sqlParmas;
		}

		private object GetDBValue(TransactionElement te, TableInfo.ColumnInfo col)
		{
			object retVal = te.Values[col.Name];

			if (col.Type == SqlDbType.Decimal || col.Type == SqlDbType.Float || 
				col.Type == SqlDbType.Real || col.Type == SqlDbType.Int)
			{
				if (col.AllowNull && string.IsNullOrEmpty(retVal.ToString()))
				{
					retVal = DBNull.Value;
				}
			}

			return retVal;
		}

		private ParameterisedSQL GetParameterisedSQLForElement(TransactionElement element)
		{

			ParameterisedSQL paramSQL;

			if (!_parameterisedQueryCache.TryGetValue(element.GetDBStatementKey(), out paramSQL))
			{
				paramSQL = CreateParameterSQLAndAddToCache(element, paramSQL);
			}

			return paramSQL;

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
					foreach (var elemValKey in element.Values.Keys)
					{
						if (table.ColumnDefinitions.ContainsKey(elemValKey)) // assumption that xml attributes and columns have same names 
						{
							includedCols.Add(table.ColumnDefinitions[elemValKey]);
							parameterBuilder.Append('@');
							parameterBuilder.Append(elemValKey + ",");
							valueBuilder.Append(elemValKey + ",");
						}
						else
						{
							Debug.Print("DB Col Missing: " + elemValKey);
						}
					}
					string insertSQL = $"INSERT INTO {table.Name} ({valueBuilder.ToString().TrimEnd(',')}) VALUES ({parameterBuilder.ToString().TrimEnd(',')})";
					paramSQL = new ParameterisedSQL(insertSQL, includedCols);
					_parameterisedQueryCache.Add(element.GetDBStatementKey(), paramSQL);

					break;

				case ActionType.Update:

					//Build update values
					foreach (var elemValKey in element.Values.Keys)
					{
						if (table.ColumnDefinitions.ContainsKey(elemValKey)) // assumption that xml attributes and columns have same names 
						{
							includedCols.Add(table.ColumnDefinitions[elemValKey]);
							if (elemValKey != element.ElementKey)
							{
								valueBuilder.Append(elemValKey);
								parameterBuilder.Append(elemValKey);
								parameterBuilder.Append(" = @");
								parameterBuilder.Append(elemValKey);
								parameterBuilder.Append(",");
							}
						}
						else
						{
							Debug.Print("DB Col Missing: " + elemValKey);
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

			internal Dictionary<string, IDBSQLParameter> Params 
			{
				get
				{
					Dictionary<string, IDBSQLParameter> retVal = new Dictionary<string, IDBSQLParameter>();
					foreach (var col in Cols)
					{
						retVal.Add(col.SQLParamName, new SQLDBParam(col.SQLParamName, col.Type, col.Length));
					}

					return retVal;
				}
			}
				
		}

		internal class TableInfo
		{
			internal string Schema { get; private set; }
			internal string Name { get; private set; }
			internal Dictionary<string, ColumnInfo> ColumnDefinitions { get; private set; } = new Dictionary<string, ColumnInfo>();
			internal TableInfo(IDBConnection dbConnection, string schema, string name)
			{
				Schema = schema;
				Name = name.ToLower();
				PopulateColumnDefinitions(dbConnection);
			}

			private void PopulateColumnDefinitions(IDBConnection dbConnection)
			{
				DataTable tableSchema = GetSchemaTable(dbConnection);

				//tableSchema.WriteXml(Path.GetTempFileName());

				foreach (DataRow row in tableSchema.Rows)
				{
					string colName = row["ColumnName"].ToString().ToLower();
					ColumnDefinitions.Add(colName, new ColumnInfo(colName, (SqlDbType)Enum.Parse(typeof(SqlDbType), row["ProviderType"].ToString()), 
						int.Parse(row["ColumnSize"].ToString()), bool.Parse(row["AllowDBNull"].ToString())));
				}
			}

			private DataTable GetSchemaTable(IDBConnection dbConnection)
			{
				return dbConnection.GetTableSchema(Name, DB_COMMAND_TIMEOUT);
			}

			internal class ColumnInfo
			{
				internal string Name { get; private set; }
				internal SqlDbType Type { get; private set; }
				internal int Length { get; private set; }
				internal bool AllowNull { get; private set; }
				internal ColumnInfo(string name, SqlDbType type, int length, bool allowNull)
				{
					Name = name.ToLower();
					Type = type;
					Length = length;
					AllowNull = allowNull;
				}
				internal string SQLParamName { get => "@" + Name; }

			}

		}
	}


}
