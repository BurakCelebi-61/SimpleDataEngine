using System.Text.RegularExpressions;

namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Represents a data version using semantic versioning
    /// </summary>
    public class DataVersion : IComparable<DataVersion>, IEquatable<DataVersion>
    {
        /// <summary>
        /// Major version number
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// Minor version number
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Patch version number
        /// </summary>
        public int Patch { get; set; }

        /// <summary>
        /// Pre-release identifier (optional)
        /// </summary>
        public string PreRelease { get; set; }

        /// <summary>
        /// Build metadata (optional)
        /// </summary>
        public string BuildMetadata { get; set; }

        /// <summary>
        /// When this version was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Initializes a new DataVersion
        /// </summary>
        /// <param name="major">Major version</param>
        /// <param name="minor">Minor version</param>
        /// <param name="patch">Patch version</param>
        /// <param name="preRelease">Pre-release identifier</param>
        /// <param name="buildMetadata">Build metadata</param>
        public DataVersion(int major = 1, int minor = 0, int patch = 0, string preRelease = null, string buildMetadata = null)
        {
            if (major < 0) throw new ArgumentException("Major version cannot be negative", nameof(major));
            if (minor < 0) throw new ArgumentException("Minor version cannot be negative", nameof(minor));
            if (patch < 0) throw new ArgumentException("Patch version cannot be negative", nameof(patch));

            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease;
            BuildMetadata = buildMetadata;
        }

        /// <summary>
        /// Parses a version string into a DataVersion
        /// </summary>
        /// <param name="version">Version string (e.g., "1.2.3", "1.0.0-alpha", "1.0.0+build.1")</param>
        /// <returns>Parsed DataVersion</returns>
        /// <exception cref="ArgumentException">Thrown when version string is invalid</exception>
        public static DataVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version string cannot be null or empty", nameof(version));

            // Regex pattern for semantic versioning
            var pattern = @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

            var match = Regex.Match(version.Trim(), pattern);

            if (!match.Success)
                throw new ArgumentException($"Invalid version format: {version}", nameof(version));

            var major = int.Parse(match.Groups["major"].Value);
            var minor = int.Parse(match.Groups["minor"].Value);
            var patch = int.Parse(match.Groups["patch"].Value);
            var preRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
            var buildMetadata = match.Groups["buildmetadata"].Success ? match.Groups["buildmetadata"].Value : null;

            return new DataVersion(major, minor, patch, preRelease, buildMetadata);
        }

        /// <summary>
        /// Tries to parse a version string
        /// </summary>
        /// <param name="version">Version string</param>
        /// <param name="result">Parsed version if successful</param>
        /// <returns>True if parsing was successful</returns>
        public static bool TryParse(string version, out DataVersion result)
        {
            try
            {
                result = Parse(version);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new version with incremented major number
        /// </summary>
        /// <returns>New version with major incremented</returns>
        public DataVersion IncrementMajor()
        {
            return new DataVersion(Major + 1, 0, 0);
        }

        /// <summary>
        /// Creates a new version with incremented minor number
        /// </summary>
        /// <returns>New version with minor incremented</returns>
        public DataVersion IncrementMinor()
        {
            return new DataVersion(Major, Minor + 1, 0);
        }

        /// <summary>
        /// Creates a new version with incremented patch number
        /// </summary>
        /// <returns>New version with patch incremented</returns>
        public DataVersion IncrementPatch()
        {
            return new DataVersion(Major, Minor, Patch + 1);
        }

        /// <summary>
        /// Checks if this version is a pre-release
        /// </summary>
        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        /// <summary>
        /// Checks if this version is stable (not pre-release)
        /// </summary>
        public bool IsStable => string.IsNullOrEmpty(PreRelease);

        /// <summary>
        /// Gets the core version without pre-release or build metadata
        /// </summary>
        public DataVersion CoreVersion => new DataVersion(Major, Minor, Patch);

        #region IComparable<DataVersion>

        public int CompareTo(DataVersion other)
        {
            if (other == null) return 1;

            // Compare major, minor, patch
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            // Pre-release versions have lower precedence than normal versions
            if (IsPreRelease && !other.IsPreRelease) return -1;
            if (!IsPreRelease && other.IsPreRelease) return 1;

            // Both are pre-release, compare pre-release identifiers
            if (IsPreRelease && other.IsPreRelease)
            {
                return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
            }

            return 0;
        }

        #endregion

        #region IEquatable<DataVersion>

        public bool Equals(DataVersion other)
        {
            if (other == null) return false;

            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   PreRelease == other.PreRelease;
        }

        #endregion

        #region Object Overrides

        public override bool Equals(object obj)
        {
            return Equals(obj as DataVersion);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch, PreRelease);
        }

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";

            if (!string.IsNullOrEmpty(PreRelease))
                version += $"-{PreRelease}";

            if (!string.IsNullOrEmpty(BuildMetadata))
                version += $"+{BuildMetadata}";

            return version;
        }

        #endregion

        #region Operators

        public static bool operator ==(DataVersion left, DataVersion right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(DataVersion left, DataVersion right)
        {
            return !(left == right);
        }

        public static bool operator <(DataVersion left, DataVersion right)
        {
            if (left is null) return right is not null;
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(DataVersion left, DataVersion right)
        {
            if (left is null) return true;
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(DataVersion left, DataVersion right)
        {
            if (left is null) return false;
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(DataVersion left, DataVersion right)
        {
            if (left is null) return right is null;
            return left.CompareTo(right) >= 0;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the minimum of two versions
        /// </summary>
        public static DataVersion Min(DataVersion version1, DataVersion version2)
        {
            if (version1 == null && version2 == null) return null;
            if (version1 == null) return version2;
            if (version2 == null) return version1;
            return version1 <= version2 ? version1 : version2;
        }

        /// <summary>
        /// Gets the maximum of two versions
        /// </summary>
        public static DataVersion Max(DataVersion version1, DataVersion version2)
        {
            if (version1 == null && version2 == null) return null;
            if (version1 == null) return version2;
            if (version2 == null) return version1;
            return version1 >= version2 ? version1 : version2;
        }

        /// <summary>
        /// Checks if a version is compatible with another (same major version)
        /// </summary>
        public static bool IsCompatible(DataVersion version1, DataVersion version2)
        {
            if (version1 == null || version2 == null) return false;
            return version1.Major == version2.Major;
        }

        #endregion
    }
}