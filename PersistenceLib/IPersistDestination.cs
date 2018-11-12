
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Persistence
{
	public interface IPersistDestination
	{
		void Connect(Dictionary<string, string> connectionParams);
		void Disconnect();
		bool IsConnected { get; }
		Task Persist(TransactionElements tes);
	}
}
