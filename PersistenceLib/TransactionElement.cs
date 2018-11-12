
using System.Collections.Generic;

namespace Persistence
{

	public enum ActionType
	{
		Unknown,
		Insert,
		Update,
		Delete
	}

	public class TransactionElement
	{

		public readonly long TransactionId;

		public readonly string ElementName;

		public readonly string ElementKey;

		public readonly ActionType Action;

		public readonly Dictionary<string, string> Values;

		private string _dbStmtKey = null;

		public TransactionElement(long transactionId, string elementName, string elementKey, ActionType action, Dictionary<string, string> attributeKeyValuePairs)
		{

			TransactionId = transactionId;

			ElementName = elementName.ToLower();

			ElementKey = elementKey.ToLower();

			Values = attributeKeyValuePairs;

			Action = action;
		}

		public string GetDBStatementKey()
		{
			if (_dbStmtKey == null)
				_dbStmtKey = $"{ElementName}_{Action}_{string.Join('.', Values.Keys)}";
			return _dbStmtKey;
		}
	}
}
