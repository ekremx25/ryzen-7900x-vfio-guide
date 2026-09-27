# Setup and recovery

## Adapt the XML

Back up the existing persistent definition first:

```sh
virsh -c qemu:///system dumpxml win11 --inactive > win11-backup.xml
```

The example is not a drop-in replacement for an existing VM. Merge relevant settings into your own domain, preserving its UUID, NVRAM and TPM state. The published UUID was removed and its MAC address replaced with an example. For a new domain, use a unique MAC and suitable firmware variable store.

Review all of the following:

- CPU numbers and SMT siblings, guest vCPU count and topology.
- Guest GPU/audio source PCI addresses (reference: `09:00.0` and `09:00.1`) and VFIO binding/IOMMU groups.
- Firmware loader, variable-store and template paths, q35 machine availability.
- Disk and CD paths under `/var/lib/libvirt/images/` and their host access permissions.
- GPU/audio ROM paths under `/var/lib/libvirt/vbios/`; firmware binaries are not included.
- Default NAT network and SPICE listener settings. Do not expose unauthenticated SPICE to an untrusted network.
- IVSHMEM ownership/access for QEMU and the Linux Looking Glass user.

Validate the edited file before defining it:

```sh
virt-xml-validate win11.xml
virsh -c qemu:///system define win11.xml --validate
```

Most hardware changes require a full normal shutdown and a new VM start. Windows Restart does not launch a new QEMU process or apply pending persistent XML changes.

## VirtIO storage migration

Do not simply switch a boot disk from SATA to SCSI after copying drivers into Windows. Our initial attempt did not boot.

1. Keep the boot disk on SATA.
2. Install the matching Windows `vioscsi` driver from the official VirtIO driver media.
3. Add a VirtIO-SCSI controller and a small temporary disk.
4. Verify the controller and disk appear without errors in Device Manager.
5. Shut Windows down normally; move the boot disk to that same controller.
6. Start the VM and verify boot before removing the temporary disk from its configuration.

The example uses `cache=none`, `io=native`, `discard=unmap`, one controller IOThread and four queues. Additional IOThreads are not automatically faster. TRIM exposure does not prove the guest has issued TRIM.

For VirtIO networking, install/verify NetKVM before replacing E1000e. The Guest Agent also requires the Windows service and VirtIO serial driver in addition to its XML channel.

## Install the GPU guard inside Windows

Start with a healthy AMD Raphael GPU. If it already reports Code 43, recover it first. Review the scripts, then copy the entire `windows-reset-guard` directory into Windows.

Right-click `INSTALL.cmd` and select **Run as administrator**. Installation compiles `Guard.cs` with the Windows .NET Framework compiler, creates the automatic LocalSystem service, and configures/verifies a 150-second preshutdown timeout through the Windows service API.

The service name/path remains `EkremAmdResetGuard` for compatibility with the tested installation. It targets PCI `VEN_1002&DEV_164E` and records a single exact device instance in `device.txt`. Do not casually widen this match.

`UPDATE.cmd` updates only `Device.ps1`, saving a timestamped backup and restarting the existing service. It does not replace the compiled service binary. A failed update retains files for inspection; review its output and backups before proceeding.

`UNINSTALL.cmd` stops the service, tries to re-enable AMD, and deletes the service only if that succeeds. Files and logs remain for recovery. If AMD cannot be enabled, inspect the log and recover the GPU before retrying.

## Test

1. Confirm AMD has no Device Manager error.
2. Shut Windows down normally; start it again without rebooting Linux.
3. Confirm AMD is healthy and Looking Glass captures frames.
4. Use Windows **Restart** and repeat the check.
5. In `guard.log`, look for `PRESHUTDOWN received`, `Disable verified, problem code=22`, and later `Enable verified, problem code=0`.

A visible SPICE fallback screen alone is not proof that AMD capture works. This setup was ultimately tested without QXL, using a cable attached to the motherboard display output. Headless virtual-display operation was not validated.

## Recovery

If a disk migration fails, restore the known-good SATA XML while the VM is off. Do not recreate or format the Windows image. Avoid forced power-off unless normal shutdown cannot complete; it bypasses GPU cleanup.

If the GPU remains in Code 43, normal guest shutdown followed by a Linux host restart recovered it on the reference system. A temporary QXL console may help diagnose guest problems, but does not reset AMD by itself. Keep motherboard display output connected when using this tested setup.

The original VFIO container error was resolved by changing a BIOS DMA-protection setting on the reference machine. This is an observation, not a general recommendation to disable DMA protection: inspect your firmware, kernel logs and IOMMU configuration first.

## Looking Glass launcher

Install the Linux B7 client and Windows B7 host according to the upstream guide. The included launcher expects VM name `win11` and `/dev/shm/looking-glass`; adjust if needed. It discovers the SPICE address/port through libvirt and requires access to the system libvirt connection.

The 256 MiB shared-memory allocation is retained from the working setup. HDR/color appearance remains an unresolved separate investigation, not a fixed feature of this repository.
