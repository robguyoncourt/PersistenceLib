
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Persistence
{
	public interface IPersistDestination: IDisposable
	{
		void Connect(Dictionary<string, string> connectionParams);
		void Disconnect();
		Task Persist(TransactionElements tes);
	}
}
