# Local patches

Upstream: https://github.com/warbler/SharpMonoInjector

Pinned commit: `73566c1be1e8e1bb25ab60683557958368b2cd47`. License notice is unchanged.

Changes in this suite:

- Source-buildable .NET Framework 4.8 x64 project; no shipped opaque old backend.
- PID-based construction, minimum needed process permissions, failed-constructor cleanup and idempotent disposal.
- Memory reads/allocations/writes limited to 32 MB per operation; string reads limited to 4 KB, batched within page boundaries; strings encoded as null-terminated UTF-8.
- Release reservations with `MEM_RELEASE`, size zero. On an uncertain remote-thread wait, preserve allocations to prevent executing freed code/data.
- Flush the instruction cache, wait at most 15 seconds for each remote call, inspect thread exit status and close each thread handle.
- Attach native threads before image loading and later Mono operations, after retrieving the root domain.
- Check exception pointers at target pointer width without dereferencing unrooted objects or guessed object offsets.
- Batch export-table reads, validate counts and ordinals, correct module information size/counts, and fail when architecture discovery fails.

The launcher only uses the injection entry point. It does not expose remote assembly ejection or arbitrary process/DLL selection. A separate launcher/session gate prevents repeated uncertain native attempts into the same game process.
