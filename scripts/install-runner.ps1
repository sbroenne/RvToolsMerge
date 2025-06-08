param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$true)]
    [string]$RepositoryUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$RunnerName
)

# Set up error handling
$ErrorActionPreference = "Stop"

# Log function
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Output "[$timestamp] $Message"
    Add-Content -Path "C:\runner-install.log" -Value "[$timestamp] $Message"
}

try {
    Write-Log "Starting GitHub Actions Runner installation..."
    
    # Create runner directory
    $runnerDir = "C:\actions-runner"
    Write-Log "Creating runner directory: $runnerDir"
    New-Item -ItemType Directory -Path $runnerDir -Force
    Set-Location $runnerDir
    
    # Download latest runner
    Write-Log "Downloading GitHub Actions Runner..."
    $latestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/actions/runner/releases/latest"
    $windowsAsset = $latestRelease.assets | Where-Object { $_.name -like "*win-x64*" }
    $downloadUrl = $windowsAsset.browser_download_url
    
    Write-Log "Download URL: $downloadUrl"
    Invoke-WebRequest -Uri $downloadUrl -OutFile "actions-runner-win-x64.zip"
    
    # Extract runner
    Write-Log "Extracting runner archive..."
    Expand-Archive -Path "actions-runner-win-x64.zip" -DestinationPath . -Force
    Remove-Item "actions-runner-win-x64.zip"
    
    # Get repository owner and name from URL
    $repoMatch = $RepositoryUrl -match "github\.com/([^/]+)/([^/]+)"
    if (-not $repoMatch) {
        throw "Invalid repository URL format: $RepositoryUrl"
    }
    $repoOwner = $Matches[1]
    $repoName = $Matches[2]
    
    # Get registration token
    Write-Log "Getting registration token from GitHub API..."
    $headers = @{
        "Authorization" = "token $GitHubToken"
        "Accept" = "application/vnd.github.v3+json"
    }
    
    $tokenResponse = Invoke-RestMethod -Uri "https://api.github.com/repos/$repoOwner/$repoName/actions/runners/registration-token" -Method POST -Headers $headers
    $registrationToken = $tokenResponse.token
    
    # Configure runner
    Write-Log "Configuring GitHub Actions Runner..."
    $configArgs = @(
        "--url", $RepositoryUrl,
        "--token", $registrationToken,
        "--name", $RunnerName,
        "--work", "_work",
        "--unattended",
        "--replace"
    )
    
    & ".\config.cmd" @configArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Runner configuration failed with exit code $LASTEXITCODE"
    }
    
    # Install as Windows service
    Write-Log "Installing runner as Windows service..."
    & ".\svc.sh" install
    
    if ($LASTEXITCODE -ne 0) {
        throw "Service installation failed with exit code $LASTEXITCODE"
    }
    
    # Start the service
    Write-Log "Starting GitHub Actions Runner service..."
    & ".\svc.sh" start
    
    if ($LASTEXITCODE -ne 0) {
        throw "Service start failed with exit code $LASTEXITCODE"
    }
    
    Write-Log "GitHub Actions Runner installation completed successfully!"
    Write-Log "Runner Name: $RunnerName"
    Write-Log "Repository: $RepositoryUrl"
    Write-Log "Service Status: Running"
    
    # Verify service is running
    $service = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq "Running") {
        Write-Log "Service verification: GitHub Actions Runner service is running"
    } else {
        Write-Log "Warning: Service verification failed - service may not be running properly"
    }
    
} catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    Write-Log "Stack trace: $($_.ScriptStackTrace)"
    
    # Try to get more details from runner logs if they exist
    $runnerLogPath = "C:\actions-runner\_diag"
    if (Test-Path $runnerLogPath) {
        Write-Log "Runner diagnostic logs found at: $runnerLogPath"
        Get-ChildItem $runnerLogPath -Filter "*.log" | ForEach-Object {
            Write-Log "Log file: $($_.Name)"
        }
    }
    
    throw
}
