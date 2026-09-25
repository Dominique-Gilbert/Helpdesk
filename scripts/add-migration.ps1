<#
.SYNOPSIS
    Adds an EF Core migration to one service.

.EXAMPLE
    pwsh ./scripts/add-migration.ps1 -Service Ticket -Name AddSlaBreachedFlag
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Ticket', 'Asset', 'Assignment', 'SLA', 'User')]
    [string] $Service,

    [Parameter(Mandatory)]
    [string] $Name
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "Backends/$Service.Infrastructure"

if (-not (Test-Path $project)) { throw "Could not find $project" }

# The design-time factory means the startup project is the class library itself, and no
# live database is needed to scaffold the migration.
dotnet ef migrations add $Name --project $project --startup-project $project --output-dir Migrations
