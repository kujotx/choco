﻿# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Install-ChocolateyDesktopLink {
<#
.SYNOPSIS
This adds a shortcut on the desktop to the specified file path.

.PARAMETER TargetFilePath
This is the location to the application/executable file that you want to add a shortcut to on the desktop.  This is mandatory.

.EXAMPLE
Install-ChocolateyDesktopLink -TargetFilePath "C:\tools\NHibernatProfiler\nhprof.exe"

This will create a new Desktop Shortcut pointing at the NHibernate Profiler exe.

#>
param(
  [string] $targetFilePath
)
  Write-Debug "Running 'Install-ChocolateyDesktopLink' with targetFilePath:`'$targetFilePath`'";
  
  if(!$targetFilePath) {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "Missing TargetFilePath input parameter."
    return
  }
  
  if(!(Test-Path($targetFilePath))) {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "TargetPath does not exist, so can't create shortcut."
    return
  }

  Write-Debug "Creating Shortcut..."
  
  try {
    $desktop = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
    $link = Join-Path $desktop "$([System.IO.Path]::GetFileName($targetFilePath)).lnk"
    $workingDirectory = $([System.IO.Path]::GetDirectoryName($targetFilePath))

    $wshshell = New-Object -ComObject WScript.Shell
    $lnk = $wshshell.CreateShortcut($link)
    $lnk.TargetPath = $targetFilePath
    $lnk.WorkingDirectory = $workingDirectory
    $lnk.Save()

    Write-Debug "Desktop Shortcut created pointing at `'$targetFilePath`'."

    Write-ChocolateySuccess "Install-ChocolateyShortcut completed"
  }	
  catch {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "There were errors attempting to create shortcut. The error message was '$_'."
  }	
}