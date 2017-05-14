using OMSServiceMapExport.Model.ServiceMap;
using System.ComponentModel.DataAnnotations;

namespace OMSServiceMapExport.Model
{
    public class MachineProcess
    {
        public int Id { get; set; }

        public Machine Machine { get; set; }
        public int MachineId { get; set; }

        [Required]
        public string ServiceMapReferenceKey {get; set;}

        public string CompanyName { get; set; }
        public string InternalName { get; set; }
        public string ProductName { get; set; }
        public string ProductVersion { get; set; }
        public string FileVersion { get; set; }
        public string CommandLine { get; set; }
        public string ExecutablePath { get; set; }
        public string WorkingDirectory { get; set; }

        public static MachineProcess CreateInstance(ProcessDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            var process = new MachineProcess()
            {
                ServiceMapReferenceKey = dto.Id
            };

            if (dto.Properties != null && dto.Properties.Details != null)
            {
                process.CommandLine = dto.Properties.Details.CommandLine;
                process.CompanyName = dto.Properties.Details.CommandLine;
                process.ExecutablePath = dto.Properties.Details.ExecutablePath;
                process.FileVersion = dto.Properties.Details.FileVersion;
                process.InternalName = dto.Properties.Details.InternalName;
                process.ProductName = dto.Properties.Details.ProductName;
                process.ProductVersion = dto.Properties.Details.ProductVersion;
                process.WorkingDirectory = dto.Properties.Details.WorkingDirectory;
            }

            return process;
        }
    }
}