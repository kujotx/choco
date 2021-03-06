﻿// Copyright © 2011 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.services
{
    using System.IO;
    using System.Text;
    using NuGet;
    using domain;
    using IFileSystem = filesystem.IFileSystem;

    internal class ChocolateyPackageInformationService : IChocolateyPackageInformationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private const string REGISTRY_SNAPSHOT_FILE = ".registry";
        private const string SILENT_UNINSTALLER_FILE = ".silentUninstaller";
        private const string SIDE_BY_SIDE_FILE = ".sxs";
        private const string PIN_FILE = ".pin";

        public ChocolateyPackageInformationService(IFileSystem fileSystem, IRegistryService registryService)
        {
            _fileSystem = fileSystem;
            _registryService = registryService;
        }

        public ChocolateyPackageInformation get_package_information(IPackage package)
        {
            var packageInformation = new ChocolateyPackageInformation(package);

            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(package.Id, package.Version.to_string()));
            if (!_fileSystem.directory_exists(pkgStorePath))
            {
                return packageInformation;
            }

            string registrySnapshotFile = _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE);
            if (_fileSystem.file_exists(registrySnapshotFile))
            {
                packageInformation.RegistrySnapshot = _registryService.read_from_file(registrySnapshotFile);
            }

            packageInformation.HasSilentUninstall = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, SILENT_UNINSTALLER_FILE));
            packageInformation.IsSideBySide = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE));
            packageInformation.IsPinned = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, PIN_FILE));

            return packageInformation;
        }

        public void save_package_information(ChocolateyPackageInformation packageInformation)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.ChocolateyPackageInfoStoreLocation);
            _fileSystem.ensure_file_attribute_set(ApplicationParameters.ChocolateyPackageInfoStoreLocation, FileAttributes.Hidden);

            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(packageInformation.Package.Id, packageInformation.Package.Version.to_string()));
            _fileSystem.create_directory_if_not_exists(pkgStorePath);

            if (packageInformation.RegistrySnapshot != null)
            {
                _registryService.save_to_file(packageInformation.RegistrySnapshot, _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE));
            }

            if (packageInformation.HasSilentUninstall)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, SILENT_UNINSTALLER_FILE), string.Empty, Encoding.ASCII);
            }
            if (packageInformation.IsSideBySide)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE), string.Empty, Encoding.ASCII);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE));
            }

            if (packageInformation.IsPinned)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, PIN_FILE), string.Empty, Encoding.ASCII);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, PIN_FILE));
            }
        }

        public void remove_package_information(IPackage package)
        {
            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(package.Id, package.Version.to_string()));
            _fileSystem.delete_directory_if_exists(pkgStorePath, recursive: true);
        }
    }
}