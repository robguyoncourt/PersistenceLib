
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

		public readonly ActionType Action;

		public readonly Dictionary<string, string> Values;

		public TransactionElement(long transactionId, string elementName, ActionType action, Dictionary<string, string> attributeKeyValuePairs)
		{

			TransactionId = transactionId;

			ElementName = elementName;

			Values = attributeKeyValuePairs;

			Action = action;
		}

	}
}
