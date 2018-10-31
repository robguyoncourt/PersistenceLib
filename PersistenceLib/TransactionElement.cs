
using System.Collections.Generic;

namespace Persistence
{
	public class TransactionElement
	{

		public readonly long TransactionId;

		public readonly string ElementName;

		public readonly string Action;

		public readonly Dictionary<string, string> Values;

		public TransactionElement(long transactionId, string elementName, string action, Dictionary<string, string> attributeKeyValuePairs)
		{

			TransactionId = transactionId;

			ElementName = elementName;

			Values = attributeKeyValuePairs;

			Action = action;
		}

	}
}
