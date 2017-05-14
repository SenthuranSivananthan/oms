﻿using OMSServiceMapExport.EF;
using OMSServiceMapExport.Model.ServiceMap;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OMSServiceMapExport.Model
{
    public class MachineInboundConnection
    {
        public int MachineInboundConnectionId { get; set; }
        [Required]
        public string ServiceMapReferenceKey { get; set; }
        public string State { get; set; }

        public Machine SourceMachine { get; set; }
        public int SourceMachineId { get; set; }
   
        public string SourceIpAddress { get; set; }
        public int SourcePort { get; set; }

        public Machine DestinationMachine { get; set; }
        public int DestinationMachineId { get; set; }

        public static MachineInboundConnection CreateInstance(ServiceMapDataContext dbContext, AcceptorDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            var inbound = new MachineInboundConnection
            {
                ServiceMapReferenceKey = dto.Id
            };

            if (dto.Properties != null)
            {
                if (dto.Properties.Source != null && dto.Properties.Source.Properties != null)
                {
                    if (dto.Properties.Source.Properties.Machine != null)
                    {
                        var machine = dbContext.Machines.Where(x => x.ServiceMapReferenceKey != null && x.ServiceMapReferenceKey.Equals(dto.Properties.Source.Properties.Machine.Id)).FirstOrDefault();
                        if (machine != null)
                        {
                            inbound.SourceMachineId = machine.MachineId;
                        }
                    }

                    inbound.SourceIpAddress = dto.Properties.Source.Properties.IpAddress;
                    inbound.SourcePort = dto.Properties.Source.Properties.PortNumber;
                }

                if (dto.Properties.Destination != null &&
                    dto.Properties.Destination.Properties != null &&
                    dto.Properties.Destination.Properties.Machine != null)
                {
                    var machine = dbContext.Machines.Where(x => x.ServiceMapReferenceKey != null && x.ServiceMapReferenceKey.Equals(dto.Properties.Destination.Properties.Machine.Id)).FirstOrDefault();
                    if (machine != null)
                    {
                        inbound.DestinationMachineId = machine.MachineId;
                    }
                }
            }

            return inbound;
        }
    }
}
