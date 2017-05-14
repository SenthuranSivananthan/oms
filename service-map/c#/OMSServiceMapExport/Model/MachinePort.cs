using OMSServiceMapExport.Model.ServiceMap;
using System.ComponentModel.DataAnnotations;

namespace OMSServiceMapExport.Model
{
    public class MachinePort
    {
        public int MachinePortId { get; set; }

        public Machine Machine { get; set; }
        public int MachineId { get; set; }

        [Required]
        public string ServiceMapReferenceKey { get; set; }
        public string State { get; set; }
        public string DisplayName { get; set; }
        public string IpAddress { get; set; }
        public int PortNumber { get; set; }

        public static MachinePort CreateInstance(PortDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            var port = new MachinePort
            {
                ServiceMapReferenceKey = dto.Id
            };

            if (dto.Properties != null)
            {
                port.State = dto.Properties.MonitoringState;
                port.DisplayName = dto.Properties.DisplayName;
                port.IpAddress = dto.Properties.IpAddress;
                port.PortNumber = dto.Properties.PortNumber;
            }

            return port;
        }
    }
}
