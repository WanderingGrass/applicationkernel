// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using System.Collections.Immutable;
using System.Net;
using Todd.Applicationkernel.Core.Abstractions;

namespace Todd.Applicationkernel.Core.Discovery
{
    public interface IServiceDiscoveryProvider
    {
        /// <summary>
        /// 初始化服务注册构建器
        /// </summary>
        Task Initialize();

        /// <summary>
        /// 注册服务 Consul通过Tags来标记版本号
        /// </summary>
        Task Register(MembershipEntry entry, TableVersion tableVersion);

        /// <summary>
        /// 注销服务
        /// </summary>
        Task Deregister(string serviceId);
        Task<MembershipTableData> ReadRow(ApplicationKernelAddress key);

        Task<MembershipTableData> ReadAll();
    }
    public sealed class TableVersion : ISpanFormattable, IEquatable<TableVersion>
    {
        /// <summary>
        /// The version part of this TableVersion. Monotonically increasing number.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// The etag of this TableVersion, used for validation of table update operations.
        /// </summary>
        public string VersionEtag { get; }

        public TableVersion(int version, string eTag)
        {
            Version = version;
            VersionEtag = eTag;
        }

        public TableVersion Next() => new(Version + 1, VersionEtag);

        public override string ToString() => $"<{Version}, {VersionEtag}>";
        string IFormattable.ToString(string format, IFormatProvider formatProvider) => ToString();

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
            => destination.TryWrite($"<{Version}, {VersionEtag}>", out charsWritten);

        public override bool Equals(object obj) => Equals(obj as TableVersion);
        public override int GetHashCode() => HashCode.Combine(Version, VersionEtag);
        public bool Equals(TableVersion other) => other is not null && Version == other.Version && VersionEtag == other.VersionEtag;
        public static bool operator ==(TableVersion left, TableVersion right) => EqualityComparer<TableVersion>.Default.Equals(left, right);
        public static bool operator !=(TableVersion left, TableVersion right) => !(left == right);
    }

    public sealed class MembershipTableData
    {
        public IReadOnlyList<Tuple<MembershipEntry, string>> Members { get; private set; }

        public TableVersion Version { get; private set; }

        public MembershipTableData(List<Tuple<MembershipEntry, string>> list, TableVersion version)
        {
            // put deads at the end, just for logging.
            list.Sort(
               (x, y) =>
               {
                   if (x.Item1.Status == ApplicationKernelStatus.Dead) return 1; // put Deads at the end
                   if (y.Item1.Status == ApplicationKernelStatus.Dead) return -1; // put Deads at the end
                   return string.CompareOrdinal(x.Item1.ApplicationKernelName, y.Item1.ApplicationKernelName);
               });
            Members = list;
            Version = version;
        }

        public MembershipTableData(Tuple<MembershipEntry, string> tuple, TableVersion version)
        {
            Members = new[] { tuple };
            Version = version;
        }

        public MembershipTableData(TableVersion version)
        {
            Members = Array.Empty<Tuple<MembershipEntry, string>>();
            Version = version;
        }

        public Tuple<MembershipEntry, string> TryGet(ApplicationKernelAddress ApplicationKernel)
        {
            foreach (var item in Members)
                if (item.Item1.ApplicationKernelAddress.Equals(ApplicationKernel))
                    return item;

            return null;
        }

        public override string ToString()
        {
            int active = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.Active);
            int dead = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.Dead);
            int created = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.Created);
            int joining = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.Joining);
            int shuttingDown = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.ShuttingDown);
            int stopping = Members.Count(e => e.Item1.Status == ApplicationKernelStatus.Stopping);

            return @$"{Members.Count} ApplicationKernels, {active} are Active, {dead} are Dead{(created > 0 ? $", {created} are Created" : null)}{(joining > 0 ? $", {joining} are Joining" : null)}{(shuttingDown > 0 ? $", {shuttingDown} are ShuttingDown" : null)}{(stopping > 0 ? $", {stopping} are Stopping" : null)}, Version={Version}. All ApplicationKernels: {Utils.EnumerableToString(Members.Select(t => t.Item1))}";
        }

        // return a copy of the table removing all dead appereances of dead nodes, except for the last one.
        public MembershipTableData WithoutDuplicateDeads()
        {
            var dead = new Dictionary<IPEndPoint, Tuple<MembershipEntry, string>>();
            // pick only latest Dead for each instance
            foreach (var next in Members.Where(item => item.Item1.Status == ApplicationKernelStatus.Dead))
            {
                var ipEndPoint = next.Item1.ApplicationKernelAddress.Endpoint;
                Tuple<MembershipEntry, string> prev;
                if (!dead.TryGetValue(ipEndPoint, out prev))
                {
                    dead[ipEndPoint] = next;
                }
                else
                {
                    // later dead.
                    if (next.Item1.ApplicationKernelAddress.Generation.CompareTo(prev.Item1.ApplicationKernelAddress.Generation) > 0)
                        dead[ipEndPoint] = next;
                }
            }
            //now add back non-dead
            List<Tuple<MembershipEntry, string>> all = dead.Values.ToList();
            all.AddRange(Members.Where(item => item.Item1.Status != ApplicationKernelStatus.Dead));
            return new MembershipTableData(all, Version);
        }

        internal Dictionary<ApplicationKernelAddress, ApplicationKernelStatus> GetApplicationKernelStatuses(Func<ApplicationKernelStatus, bool> filter, bool includeMyself, ApplicationKernelAddress myAddress)
        {
            var result = new Dictionary<ApplicationKernelAddress, ApplicationKernelStatus>();
            foreach (var memberEntry in this.Members)
            {
                var entry = memberEntry.Item1;
                if (!includeMyself && entry.ApplicationKernelAddress.Equals(myAddress)) continue;
                if (filter(entry.Status)) result[entry.ApplicationKernelAddress] = entry.Status;
            }

            return result;
        }
    }
    public sealed class MembershipEntry
    {
        public ApplicationKernelAddress ApplicationKernelAddress { get; set; }

        public ApplicationKernelStatus Status { get; set; }
        public List<Tuple<ApplicationKernelAddress, DateTime>> SuspectTimes { get; set; }

        public int ProxyPort { get; set; }

        public string HostName { get; set; }

        public string ApplicationKernelName { get; set; }


        public DateTime StartTime { get; set; }

        public DateTime IAmAliveTime { get; set; }

        internal DateTime EffectiveIAmAliveTime
        {
            get
            {
                var startTimeUtc = DateTime.SpecifyKind(StartTime, DateTimeKind.Utc);
                var iAmAliveTimeUtc = DateTime.SpecifyKind(IAmAliveTime, DateTimeKind.Utc);
                return startTimeUtc > iAmAliveTimeUtc ? startTimeUtc : iAmAliveTimeUtc;
            }
        }

        public void AddOrUpdateSuspector(ApplicationKernelAddress localApplicationKernel, DateTime voteTime, int maxVotes)
        {
            var allVotes = SuspectTimes ??= new List<Tuple<ApplicationKernelAddress, DateTime>>();

            // Find voting place:
            //      update my vote, if I voted previously
            //      OR if the list is not full - just add a new vote
            //      OR overwrite the oldest entry.
            int indexToWrite = allVotes.FindIndex(voter => localApplicationKernel.Equals(voter.Item1));
            if (indexToWrite == -1)
            {
                // My vote is not recorded. Find the most outdated vote if the list is full, and overwrite it
                if (allVotes.Count >= maxVotes) // if the list is full
                {
                    // The list is full, so pick the most outdated value to overwrite.
                    DateTime minVoteTime = allVotes.Min(voter => voter.Item2);

                    // Only overwrite an existing vote if the local time is greater than the current minimum vote time.
                    if (voteTime >= minVoteTime)
                    {
                        indexToWrite = allVotes.FindIndex(voter => voter.Item2.Equals(minVoteTime));
                    }
                }
            }

            if (indexToWrite == -1)
            {
                AddSuspector(localApplicationKernel, voteTime);
            }
            else
            {
                var newEntry = new Tuple<ApplicationKernelAddress, DateTime>(localApplicationKernel, voteTime);
                SuspectTimes[indexToWrite] = newEntry;
            }
        }

        public void AddSuspector(ApplicationKernelAddress suspectingApplicationKernel, DateTime suspectingTime)
            => (SuspectTimes ??= new()).Add(Tuple.Create(suspectingApplicationKernel, suspectingTime));

        internal MembershipEntry Copy()
        {
            var copy = new MembershipEntry
            {
                ApplicationKernelAddress = ApplicationKernelAddress,
                Status = Status,
                SuspectTimes = SuspectTimes is null ? null : new(SuspectTimes),
                ProxyPort = ProxyPort,
                HostName = HostName,
                ApplicationKernelName = ApplicationKernelName,
                StartTime = StartTime,
                IAmAliveTime = IAmAliveTime
            };

            return copy;
        }

        internal MembershipEntry WithStatus(ApplicationKernelStatus status)
        {
            var updated = this.Copy();
            updated.Status = status;
            return updated;
        }

        internal MembershipEntry WithIAmAliveTime(DateTime iAmAliveTime)
        {
            var updated = this.Copy();
            updated.IAmAliveTime = iAmAliveTime;
            return updated;
        }

        internal ImmutableList<Tuple<ApplicationKernelAddress, DateTime>> GetFreshVotes(DateTime now, TimeSpan expiration)
        {
            if (this.SuspectTimes == null)
                return ImmutableList<Tuple<ApplicationKernelAddress, DateTime>>.Empty;

            var result = ImmutableList.CreateBuilder<Tuple<ApplicationKernelAddress, DateTime>>();

            // Find the latest time from the set of suspect times and the local time.
            // This prevents local clock skew from resulting in a different tally of fresh votes.
            var recencyWindowEndTime = Max(now, SuspectTimes);
            foreach (var voter in this.SuspectTimes)
            {
                // If now is smaller than otherVoterTime, than assume the otherVoterTime is fresh.
                // This could happen if clocks are not synchronized and the other voter clock is ahead of mine.
                var suspectTime = voter.Item2;
                if (recencyWindowEndTime.Subtract(suspectTime) < expiration)
                {
                    result.Add(voter);
                }
            }

            return result.ToImmutable();

            static DateTime Max(DateTime localTime, List<Tuple<ApplicationKernelAddress, DateTime>> suspectTimes)
            {
                var maxValue = localTime;
                foreach (var entry in suspectTimes)
                {
                    var suspectTime = entry.Item2;
                    if (suspectTime > maxValue) maxValue = suspectTime;
                }

                return maxValue;
            }
        }

        public override string ToString() => $"ApplicationKernelAddress={ApplicationKernelAddress} ApplicationKernelName={ApplicationKernelName} Status={Status}";
    }

    public enum ApplicationKernelStatus
    {
        /// <summary>
        /// No known status.
        /// </summary>
        None = 0,

        /// <summary>
        /// This ApplicationKernel was just created, but not started yet.
        /// </summary>
        Created = 1,

        /// <summary>
        /// This ApplicationKernel has just started, but not ready yet. It is attempting to join the cluster.
        /// </summary>
        Joining = 2,

        /// <summary>
        /// This ApplicationKernel is alive and functional.
        /// </summary>
        Active = 3,

        /// <summary>
        /// This ApplicationKernel is shutting itself down.
        /// </summary>
        ShuttingDown = 4,

        /// <summary>
        /// This ApplicationKernel is stopping itself down.
        /// </summary>
        Stopping = 5,

        /// <summary>
        /// This ApplicationKernel is deactivated/considered to be dead.
        /// </summary>
        Dead = 6
    }

    /// <summary>
    /// Extensions for <see cref="ApplicationKernelStatus"/>.
    /// </summary>
    public static class ApplicationKernelStatusExtensions
    {
        /// <summary>
        /// Return true if this ApplicationKernel is currently terminating: ShuttingDown, Stopping or Dead.
        /// </summary>
        /// <param name="ApplicationKernelStatus">The ApplicationKernel status.</param>
        /// <returns><c>true</c> if the specified ApplicationKernel status is terminating; otherwise, <c>false</c>.</returns>
        public static bool IsTerminating(this ApplicationKernelStatus ApplicationKernelStatus)
        {
            return ApplicationKernelStatus == ApplicationKernelStatus.ShuttingDown || ApplicationKernelStatus == ApplicationKernelStatus.Stopping || ApplicationKernelStatus == ApplicationKernelStatus.Dead;
        }
    }

}
