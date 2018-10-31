using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence
{
	public class TransactionElements
	{
		public long TransactionId { get; private set; }

		public List<TransactionElement> Elements { get; private set; }

		public TransactionElements(long transactionId)
		{
			TransactionId = transactionId;
			Elements = new List<TransactionElement>();
		}
	}
}
