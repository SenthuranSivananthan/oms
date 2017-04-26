$Settings = @{"workspaceId"="OMS_WORKSPACE_ID"};
$ProtectedSettings = @{"workspaceKey"="OMS_WORKSPACE_KEY"};

$OMSExistingInstalledCount = 0
$OMSNewInstallWindowsCount = 0
$OMSNewInstallLinuxCount = 0
$OMSNewInstallVMOffCount = 0

# Set to false when you want to actually install the OMS Extension
$DryRun = $true

$ResourceGroupList = Get-AzureRmResourceGroup -Verbose

foreach ($ResourceGroupName in $ResourceGroupList.ResourceGroupName)
{
    Write-Host("Checking " + $ResourceGroupName)

    $VMList = Get-AzureRmVM -ResourceGroupName $ResourceGroupName -WarningAction SilentlyContinue -Verbose

    ForEach ($VM in $VMList)
    {
        $IsVMExtensionInstalled = Get-AzureRmVMExtension `
                                        -ResourceGroupName $ResourceGroupName `
                                        -VMName $VM.Name `
                                        -Name MicrosoftMonitoringAgent `
                                        -ErrorAction SilentlyContinue `
                                        -WarningAction SilentlyContinue

        if ($IsVMExtensionInstalled -eq $null)
        {
            $VMPowerState = $VM | Get-AzureRmVM -Status -WarningAction SilentlyContinue | Select -ExpandProperty Statuses | ?{ $_.Code -match "PowerState" } | Select -ExpandProperty DisplayStatus

            if ($VMPowerState -eq "VM running")
            {
                if ($VM.StorageProfile.OsDisk.OsType -eq "Windows"){
                    if ($DryRun -eq $false)
                    {
                        "Installing OMS agent on Windows Server: " + $VM.Name

                        Set-AzureRmVMExtension `
                                -ExtensionType MicrosoftMonitoringAgent `
                                -Name MicrosoftMonitoringAgent `
                                -Publisher Microsoft.EnterpriseCloud.Monitoring `
                                -ResourceGroupName $ResourceGroupName `
                                -TypeHandlerVersion 1.0 `
                                -VMName $VM.Name `
                                -Location $VM.Location `
                                -ProtectedSettings $ProtectedSettings `
                                -Settings $Settings `
                                -Verbose
                    }

                    $OMSNewInstallWindowsCount++
                }
                else
                {
                    if ($DryRun -eq $false)
                    {
                        "Installing OMS agent on Linux Server: " + $VM.Name

                        Set-AzureRmVMExtension `
                                -ExtensionType OmsAgentForLinux `
                                -Name OmsAgentForLinux `
                                -Publisher Microsoft.EnterpriseCloud.Monitoring `
                                -ResourceGroupName $ResourceGroupName `
                                -TypeHandlerVersion 1.0 `
                                -VMName $VM.Name `
                                -Location $VM.Location `
                                -ProtectedSettings $ProtectedSettings `
                                -Settings $Settings `
                                -Verbose
                    }

                    $OMSNewInstallLinuxCount++
                }
            }
            else
            {
                $OMSNewInstallVMOffCount++
            }
        }
        else
        {
            $OMSExistingInstalledCount++
        }
    }
}

Write-Host("*****************************")

if ($DryRun -eq $true)
{
    Write-Host("This script was executed in Dry Run mode.  OMS Extension was not installed.")
}

Write-Host("VMs with OMS Agent: " + $OMSExistingInstalledCount + " server(s)")
Write-Host("OMS (Windows Agent) newly installed: " + $OMSNewInstallWindowsCount + " server(s)")
Write-Host("OMS (Linux Agent) newly installed: " + $OMSNewInstallLinuxCount + " server(s)")
Write-Host("OMS agent not installed due to Power Off: " + $OMSNewInstallVMOffCount + " server(s)")