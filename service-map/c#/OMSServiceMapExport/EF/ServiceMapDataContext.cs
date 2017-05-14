using OMSServiceMapExport.Model;
using System.Data.Entity;

namespace OMSServiceMapExport.EF
{
    public class ServiceMapDataContext : DbContext
    {
        public ServiceMapDataContext()
            : base("ServiceMapDatabase")
        {
        }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineProcess> MachineProcesses { get; set; }
    }
}
