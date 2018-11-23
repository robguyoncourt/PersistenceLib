using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Persistence
{
	public static class DBConnectionParams
	{
		public static readonly string SERVER = "SERVER";
		public static readonly string DATABASE = "DATABASE";
		public static readonly string USERID = "USERID";
		public static readonly string PASSWORD = "PASSWORD";
	}

	public interface IDBConnection : IDisposable
	{ 
		void Open(Dictionary<string, string> connectionParams);
		void Close();
		bool IsConnected { get; }
		IDBTransaction BeginTransaction();
		DataTable ExecuteQuery(string sql, int timeout);
		DataTable GetTableSchema(string tableName, int timeout);
	}

	public interface IDBTransaction : IDisposable
	{
		void Commit();
		void Rollback();
		void ExecuteNonQuery(string sql, List<IDBSQLParameter>sqlParams, int timeout);
	}

	public interface IDBSQLParameter 
	{
		string Name { get;  }
		object DBType { get; }
		int Length { get;  }
		object Value { get; set; }
	}

}
