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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using NuGet;
    using configuration;
    using logging;

    // ReSharper disable InconsistentNaming

    public sealed class NugetCommon
    {
        public static IFileSystem GetNuGetFileSystem(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            var fileSystem = new ChocolateyPhysicalFileSystem(ApplicationParameters.PackagesLocation);
            if (configuration.Debug)
            {
                fileSystem.Logger = nugetLogger;
            }

            return fileSystem;
        }

        public static IPackagePathResolver GetPathResolver(ChocolateyConfiguration configuration, IFileSystem nugetPackagesFileSystem)
        {
            return new ChocolateyPackagePathResolver(nugetPackagesFileSystem, configuration.AllowMultipleVersions);
        }

        public static IPackageRepository GetLocalRepository(IPackagePathResolver pathResolver, IFileSystem nugetPackagesFileSystem)
        {
            IPackageRepository localRepository = new ChocolateyLocalPackageRepository(pathResolver, nugetPackagesFileSystem);
            localRepository.PackageSaveMode = PackageSaveModes.Nupkg;

            return localRepository;
        }

        public static IPackageRepository GetRemoteRepository(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            IEnumerable<string> sources = configuration.Sources.Split(new[] {";", ","}, StringSplitOptions.RemoveEmptyEntries);

            IList<IPackageRepository> repositories = new List<IPackageRepository>();
            foreach (var source in sources.or_empty_list_if_null())
            {
                try
                {
                    var uri = new Uri(source);
                    if (uri.IsFile || uri.IsUnc)
                    {
                        repositories.Add(new ChocolateyLocalPackageRepository(uri.LocalPath));
                    }
                    else
                    {
                        repositories.Add(new DataServicePackageRepository(uri));
                    }
                }
                catch (Exception)
                {
                    repositories.Add(new ChocolateyLocalPackageRepository(source));
                }
            }

            //todo well that didn't work on failing repos... grrr
            var repository = new AggregateRepository(repositories) {IgnoreFailingRepositories = true};
            //,ResolveDependenciesVertically = true};
            if (configuration.Debug)
            {
                repository.Logger = nugetLogger;
            }

            return repository;
        }

        public static IPackageManager GetPackageManager(ChocolateyConfiguration configuration, ILogger nugetLogger, Action<PackageOperationEventArgs> installSuccessAction, Action<PackageOperationEventArgs> uninstallSuccessAction)
        {
            IFileSystem nugetPackagesFileSystem = GetNuGetFileSystem(configuration, nugetLogger);
            IPackagePathResolver pathResolver = GetPathResolver(configuration, nugetPackagesFileSystem);
            var packageManager = new PackageManager(GetRemoteRepository(configuration, nugetLogger), pathResolver, nugetPackagesFileSystem, GetLocalRepository(pathResolver, nugetPackagesFileSystem))
                {
                    DependencyVersion = DependencyVersion.Highest,
                };

            if (configuration.Debug)
            {
                packageManager.Logger = nugetLogger;
            }

            packageManager.PackageInstalled += (s, e) =>
                {
                    var pkg = e.Package;
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}".format_with(Environment.NewLine, pkg.Id, pkg.Version.to_string(), configuration.Force ? " (forced)" : string.Empty));

                    if (installSuccessAction != null) installSuccessAction.Invoke(e);
                };

            packageManager.PackageUninstalled += (s, e) =>
                {
                    IPackage pkg = packageManager.LocalRepository.FindPackage(e.Package.Id, e.Package.Version);
                    if (pkg != null)
                    {
                        // install not actually removed, let's clean it up. This is a bug with nuget, where it reports it removed some package and did NOTHING
                        // this is what happens when you are switching from AllowMultiple to just one and back
                        var chocoPathResolver = packageManager.PathResolver as ChocolateyPackagePathResolver;
                        if (chocoPathResolver != null)
                        {
                            chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;
                            packageManager.UninstallPackage(pkg, forceRemove: configuration.Force, removeDependencies: false);
                            chocoPathResolver.UseSideBySidePaths = configuration.AllowMultipleVersions;
                        }
                    }
                    else
                    {
                        if (uninstallSuccessAction != null) uninstallSuccessAction.Invoke(e);
                    }
                };

            return packageManager;
        }
    }

    // ReSharper restore InconsistentNaming
}