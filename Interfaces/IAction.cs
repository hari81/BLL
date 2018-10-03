using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Repositories;
using BLL.Core.Domain;

namespace BLL.Interfaces
{
    public interface IAction:IDisposable
    {
        int UniqueId { get; }
        IEquipmentActionRecord _actionRecord { get; }
        ActionStatus Status { get;}
        string ActionLog { get; }
        string Message { get; }
        ActionStatus Start();
        ActionStatus Validate();
        ActionStatus Cancel();
        ActionStatus Commit();
    }
}
