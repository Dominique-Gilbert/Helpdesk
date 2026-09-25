<#
.SYNOPSIS
    Proves the Ticket.Created fan-out end to end.

.DESCRIPTION
    Creates a ticket through the REST gateway, then polls until BOTH an assignment
    (Assignment.API) and an SLA clock (SLA.API) exist for it. Two services, two databases,
    one event, neither aware of the other.

    This is the evidence for the PR's "Testing and Verification" section - run it after
    `docker compose up` and paste the output.

.EXAMPLE
    pwsh ./scripts/verify-fanout.ps1
    pwsh ./scripts/verify-fanout.ps1 -Gateway http://localhost:8080 -Priority Critical
#>

[CmdletBinding()]
param(
    [string] $Gateway = 'http://localhost:8080',
    [string] $Category = 'Hardware',
    [ValidateSet('Low', 'Normal', 'High', 'Critical')]
    [string] $Priority = 'High',
    [int] $TimeoutSeconds = 45,
    [string] $Username = 'admin',
    [string] $Password = 'Passw0rd!'
)

# Admin, not a seeded Technician: GET /assignment/ticket/{id} is Admin/BaseRole-only by design
# (see AssignmentController.GetForTicket), and SLA.API's own scoping only shows a Technician
# clocks for tickets assigned to *them* - this script's ticket can land on any technician the
# router picks, so polling as a role that can see every ticket is the only account shape that
# is guaranteed to work regardless of which technician gets assigned.

$ErrorActionPreference = 'Stop'

function Step { param($Text) Write-Host "`n-> $Text" -ForegroundColor Cyan }
function Pass { param($Text) Write-Host "   PASS  $Text" -ForegroundColor Green }
function Fail { param($Text) Write-Host "   FAIL  $Text" -ForegroundColor Red }

Step "Gateway readiness"
try {
    Invoke-RestMethod -Uri "$Gateway/health/readiness" -TimeoutSec 10 | Out-Null
    Pass "$Gateway is ready"
} catch {
    Fail "gateway is not answering at $Gateway - has 'docker compose up' finished?"
    exit 1
}

# Every endpoint but User.API's login requires a bearer token (see JwtAuthExtensions'
# fallback policy) - log in with a seeded demo account before touching anything else.
Step "Log in as '$Username'"
$loginBody = @{ username = $Username; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$Gateway/user/login" -Method Post -Body $loginBody -ContentType 'application/json'
$authHeaders = @{ Authorization = "Bearer $($login.token)" }
Pass "authenticated as $($login.fullName) ($($login.role))"

Step "Create a ticket (the only write in this script)"
$body = @{
    title = "Fan-out check $(Get-Date -Format 'HH:mm:ss')"
    description = 'Created by scripts/verify-fanout.ps1'
    category = $Category
    priority = $Priority
    requestedBy = 'verify-fanout'
} | ConvertTo-Json

$ticket = Invoke-RestMethod -Uri "$Gateway/ticket" -Method Post -Body $body -ContentType 'application/json' -Headers $authHeaders
Pass "$($ticket.reference) created ($($ticket.id))"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$assignment = $null
$slaClock = $null

Step "Waiting for both consumers (timeout ${TimeoutSeconds}s)"
while ((Get-Date) -lt $deadline -and (-not $assignment -or -not $slaClock)) {
    if (-not $assignment) {
        try { $assignment = Invoke-RestMethod -Uri "$Gateway/assignment/ticket/$($ticket.id)" -Headers $authHeaders } catch { }
    }
    if (-not $slaClock) {
        try { $slaClock = Invoke-RestMethod -Uri "$Gateway/sla/ticket/$($ticket.id)" -Headers $authHeaders } catch { }
    }
    if (-not $assignment -or -not $slaClock) { Start-Sleep -Milliseconds 500 }
}

$ok = $true

if ($assignment) {
    Pass "Assignment.API  -> $($assignment.technicianName) ($($assignment.team))"
    Write-Host "         reason: $($assignment.explanation)" -ForegroundColor DarkGray
} else {
    Fail 'Assignment.API never produced an assignment'
    $ok = $false
}

if ($slaClock) {
    Pass "SLA.API         -> $($slaClock.status) clock, due $($slaClock.dueAtUtc)"
} else {
    Fail 'SLA.API never started a clock'
    $ok = $false
}

Step 'Aggregated view through the gateway'
$overview = Invoke-RestMethod -Uri "$Gateway/ticket/$($ticket.id)/overview" -Headers $authHeaders
if ($overview.isFullyProcessed) {
    Pass 'One event, two independent consumers, both visible in one DTO'
} else {
    Fail 'The overview still reports the ticket as not fully processed'
    $ok = $false
}

Write-Host ''
if ($ok) {
    Write-Host 'FAN-OUT VERIFIED' -ForegroundColor Green
    Write-Host 'Now try it the other way: `docker compose stop assignment-api`, run this again,' -ForegroundColor DarkGray
    Write-Host 'and confirm the SLA clock still starts while the assignment check fails.' -ForegroundColor DarkGray
    exit 0
}

Write-Host 'FAN-OUT NOT VERIFIED' -ForegroundColor Red
exit 1
