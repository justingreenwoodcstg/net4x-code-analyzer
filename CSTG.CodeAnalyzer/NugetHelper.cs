using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSTG.CodeAnalyzer.Model;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace CSTG.CodeAnalyzer
{
    public class NugetHelper
    {

        public static async Task LookUpPackage(NugetPackage package)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");


            PackageMetadataResource metaDataResource = await repository.GetResourceAsync<PackageMetadataResource>();
            IEnumerable<IPackageSearchMetadata> packageMetaData = await metaDataResource.GetMetadataAsync(
                package.Id,
                includePrerelease: true,
                includeUnlisted: true,
                cache,
                logger,
                cancellationToken);

            //PackageSearchResource searchResource = await repository.GetResourceAsync<PackageSearchResource>();
            //IEnumerable<IPackageSearchMetadata> packages = await searchResource.SearchAsync(
            //  "json",
            //    searchFilter,
            //    skip: 0,
            //    take: 20,
            //    logger,
            //    cancellationToken);
            //NuGet.Protocol.Core.Types.DependencyInfoResource
            //NuGet.Protocol.Core.Types.PackageUpdateResource
            //NuGet.Protocol.Core.Types.ListResource

            var matchingMetaData = packageMetaData.FirstOrDefault(metaData => metaData.Identity.Version == NuGetVersion.Parse(package.Version.ToString()));
            if (matchingMetaData != null)
            {
                package.VersionDetails = Map(matchingMetaData);
                var xresource = await repository.GetResourceAsync<ListResource>();

                FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
                IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                    package.Id,
                    cache,
                    logger,
                    cancellationToken);

                var matchingVersion = versions.Where(v => v.Version > matchingMetaData.Identity.Version.Version && !v.IsPrerelease).OrderByDescending(x => x.Version).FirstOrDefault() 
                    ?? versions.Where(v => v.Version > matchingMetaData.Identity.Version.Version).OrderByDescending(x => x.Version).FirstOrDefault();
                if (matchingVersion != null)
                {
                    package.LatestVersionDetails = Map(packageMetaData.FirstOrDefault(x => x.Identity.Version.Version == matchingVersion.Version));
                } 
                else
                {
                    package.LatestVersionDetails = package.VersionDetails;
                }
            }
        }

        private static bool AreVersionsEqual(Version v1, Version v2)
        {
            if (v1.Major != -1 && v2.Major != -1 && v1.Major != v2.Major) return false;
            if (v1.Minor != -1 && v2.Minor != -1 && v1.Minor != v2.Minor) return false;
            if (v1.MajorRevision != -1 && v2.MajorRevision != -1 && v1.MajorRevision != v2.MajorRevision) return false;
            if (v1.MinorRevision != -1 && v2.MinorRevision != -1 && v1.MinorRevision != v2.MinorRevision) return false;
            if (v1.Revision != -1 && v2.Revision != -1 && v1.Revision != v2.Revision) return false;
            if (v1.Build != -1 && v2.Build != -1 && v1.Build != v2.Build) return false;
            return true;
        }

        private static NugetPackageVersion Map(IPackageSearchMetadata metaData)
        {
            if (metaData == null) return null;
            var v = new NugetPackageVersion
            {
                Title = metaData.Title ?? metaData.Identity.Id,
                Authors = metaData.Authors,
                PackageUrl = metaData.PackageDetailsUrl?.ToString(),
                ProjectUrl = metaData.ProjectUrl?.ToString(),
                DatePublished = metaData.Published,
                Tags = metaData.Tags,
                Summary = metaData.Summary,
                Description = metaData.Description,
                Version = metaData.Identity.Version.Version
            };
            return v;
        }
    }
}
