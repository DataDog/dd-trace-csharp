Configuration DatadogApmDotnet
{
    # Adapted from https://github.com/DataDog/dd-trace-dotnet/blob/lpimentel/powershell-dsc/tools/PowerShell-DSC/DatadogApmDotnet.ps1
    param
    (
        # Target nodes to apply the configuration
        [Parameter(Mandatory=$false)][string]$NodeName = 'localhost',
    )

    Import-DscResource -ModuleName PSDscResources -Name MsiPackage
    Import-DscResource -ModuleName PSDscResources -Name Environment

    # Version of the Agent package to be installed
    $AgentVersion = '7.27.0'

    # Version of the Tracer package to be installed
    $TracerVersion = '1.26.1'

    Node "localhost"
    {
        MsiPackage 'dd-trace-dotnet' {
            Path      = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TracerVersion/datadog-dotnet-apm-$TracerVersion-x64.msi"
            ProductId = '00B19BDB-EC40-4ADF-A73F-789A7721247A'
            Ensure    = 'Present'
        }

        Environment 'COR_PROFILER' {
            Name   = 'COR_PROFILER'
            Value  = '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
            Ensure = 'Present'
            Path   = $false
            Target = @('Machine')
        }

        Environment 'CORECLR_PROFILER' {
            Name   = 'CORECLR_PROFILER'
            Value  = '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}'
            Ensure = 'Present'
            Path   = $false
            Target = @('Machine')
        }
    }
}