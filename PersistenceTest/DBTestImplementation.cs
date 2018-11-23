using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace Persistence.UnitTests
{
	public class DBTestConnection : IDBConnection
	{
		private bool _isConnected = false;
		private bool _isDisposed = false;

		public Dictionary<string, string> ConnectionParams{get; private set;}

		public readonly List<List<(string sql, List<IDBSQLParameter> parameters)>> CommittedTransactions;

		public bool IsConnected => _isConnected;

		public bool IsDisposed {get => _isDisposed;}

		public DBTestConnection()
		{
			CommittedTransactions = new List<List<(string sql, List<IDBSQLParameter> parameters)>>();
		}

		public IDBTransaction BeginTransaction()
		{
			List<(string sql, List<IDBSQLParameter> parameters)> transaction = new List<(string sql, List<IDBSQLParameter> parameters)>();
			return new DBTestTransaction(CommittedTransactions, transaction);
		}

		public void Close()
		{
			_isConnected = false;
		}

		public void Dispose()
		{
			_isDisposed = true;
		}

		public DataTable ExecuteQuery(string sql, int timeout)
		{
			throw new NotImplementedException();
		}

		public DataTable GetTableSchema(string tableName, int timeout)
		{
			DataTable tab = new DataTable(tableName);
			if (tableName == "orders")
			{
				DataSet ds = new DataSet();
				ds.ReadXml(TestHelper.GetManifestFileAsStream("OrderDBSchema.xml"));
				tab = ds.Tables[0];
			}

			return tab;
		}

		public void Open(Dictionary<string, string> connectionParams)
		{
			ConnectionParams = connectionParams;
			_isConnected = true;
		}
	}

	public class DBTestTransaction : IDBTransaction
	{
		private bool _isDisposed = false;

		public bool IsDisposed { get => _isDisposed; }

		private readonly List<(string sql, List<IDBSQLParameter> parameters)> _queries;
		private readonly List<List<(string sql, List<IDBSQLParameter> parameters)>> _committed;
		public DBTestTransaction(List<List<(string sql, List<IDBSQLParameter> parameters)>> committed, List<(string sql, List<IDBSQLParameter> parameters)> queries)
		{
			_committed = committed;
			_queries = queries;
		}
		public void Commit()
		{
			_committed.Add(_queries);
		}

		public void Dispose()
		{
			_isDisposed = true;
		}

		public void ExecuteNonQuery(string sql, List<IDBSQLParameter> sqlParams, int timeout)
		{
			_queries.Add((sql: sql, parameters: sqlParams));
		}

		public void Rollback()
		{
			_queries.Clear();
		}
	}

}
