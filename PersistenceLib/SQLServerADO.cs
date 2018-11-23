using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Persistence
{
	public class SQLServerADOConnection : IDBConnection
	{
		private readonly SqlConnection _dbConnection;
		private bool _disposed = false;

		public SQLServerADOConnection()
		{
			_dbConnection = new SqlConnection();
		}

		public bool IsConnected =>  _dbConnection.State == ConnectionState.Executing ||
									_dbConnection.State == ConnectionState.Fetching ||
									_dbConnection.State == ConnectionState.Open;

		public IDBTransaction BeginTransaction()
		{
			return new SQLServerADOTransaction(_dbConnection);
		}

		public void Close()
		{
			_dbConnection.Close();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			if (_dbConnection != null)
			{
				if (this.IsConnected)
					_dbConnection.Close();

				_dbConnection.Dispose();
			}
	
			_disposed = true;
		}

		public DataTable ExecuteQuery(string sql, int timeout)
		{
			SqlCommand cmd = _dbConnection.CreateCommand();
			cmd.CommandTimeout = timeout;
			cmd.CommandText = sql;
			using (SqlDataReader rdr = cmd.ExecuteReader())
			{
				return rdr.GetSchemaTable();
			}
		}

		public DataTable GetTableSchema(string tableName, int timeout)
		{
			return ExecuteQuery("SET FMTONLY ON; SELECT* FROM " + tableName + "; SET FMTONLY OFF", timeout);
		}

		public void Open(Dictionary<string, string> connectionParams)
		{
			const string DB_CONNECTION_STRING = "Server={0};Database={1};User Id={2};Password={3};Integrated Security=False";

			_dbConnection.ConnectionString = string.Format(DB_CONNECTION_STRING, 
				connectionParams[DBConnectionParams.SERVER], 
				connectionParams[DBConnectionParams.DATABASE],
				connectionParams[DBConnectionParams.USERID],
				connectionParams[DBConnectionParams.PASSWORD]);

			_dbConnection.Open();
		}

	}

	public class SQLServerADOTransaction : IDBTransaction
	{
		private readonly SqlTransaction _sqlTransaction;
		private readonly SqlConnection _sqlConnection;

		private bool _disposed = false;

		public SQLServerADOTransaction(SqlConnection sqlConnection)
		{
			_sqlConnection = sqlConnection;
			_sqlTransaction = sqlConnection.BeginTransaction();
		}

		public void Commit()
		{
			_sqlTransaction.Commit();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_sqlTransaction?.Dispose();

			_disposed = true;
		}

		public void ExecuteNonQuery(string sql, List<IDBSQLParameter> sqlParams, int timeout)
		{
			using (SqlCommand cmd = _sqlConnection.CreateCommand())
			{
				cmd.CommandTimeout = timeout;
				cmd.Transaction = _sqlTransaction;
				cmd.CommandText = sql;

				List<SqlParameter> sps = new List<SqlParameter>();
				foreach (var item in sqlParams)
				{
					SqlParameter sp = new SqlParameter(item.Name, (SqlDbType)item.DBType, item.Length);
					sp.Value = item.Value;
					sps.Add(sp);
				}
				cmd.Parameters.AddRange(sps.ToArray());
				cmd.ExecuteNonQuery();

			}
		}

		public void Rollback()
		{
			_sqlTransaction.Rollback();
		}
	}

	public class SQLDBParam : IDBSQLParameter
	{
		private readonly string _name;
		private readonly SqlDbType _dbType;
		private readonly int _length;
		private object _value;
		public SQLDBParam(string name, SqlDbType dbType, int length)
		{
			_name = name;
			_dbType = dbType;
			_length = length;
		}

		public string Name { get => _name;}
		public object DBType { get => _dbType; }
		public int Length { get => _length;  }
		public object Value { get => _value; set => _value = value; }
	}

}
