# Ryzen 7900X iGPU passthrough: Windows 11 on Arch Linux

A documented working configuration for passing an AMD Raphael integrated GPU to a Windows 11 libvirt VM, viewing it through Looking Glass B7, and working around a reproducible GPU reset/Code 43 problem.

This is a hardware-specific reference, not a universal installer. The reset workaround was validated by normal Windows shutdown/start and restart on the author's machine. It does not implement a hardware reset or guarantee recovery on other systems. No performance improvement percentages have been measured.

## Reference hardware and configuration

| Component | Configuration |
|---|---|
| CPU | AMD Ryzen 9 7900X, 12 cores / 24 threads |
| Host GPU | Radeon RX 9070 XT |
| Guest GPU | Raphael iGPU, PCI ID `1002:164e` |
| Motherboard | MSI B840 GAMING PLUS WIFI |
| Host | Arch Linux, CachyOS kernel, libvirt/QEMU |
| Guest | Windows 11, AMD display driver, VirtIO drivers 0.1.302 |
| Display transport | Looking Glass B7, 256 MiB IVSHMEM, motherboard display cable connected |
| Guest CPU | 1 socket, 6 cores, 2 threads; 12 vCPUs |
| Guest RAM | Approximately 11.91 GiB |
| Storage | Raw image on ext4/NVMe; VirtIO-SCSI, 4 queues, 1 IOThread |
| Network | VirtIO-net, vhost, 4 queues, libvirt default NAT |

## Files

- [`config/win11.example.xml`](config/win11.example.xml): sanitized persistent domain XML.
- [`windows-reset-guard/`](windows-reset-guard/): C# Windows service and PowerShell installation/device scripts.
- [`scripts/looking-glass-win11`](scripts/looking-glass-win11): Linux launcher that discovers the VM's SPICE endpoint.
- [`docs/setup.md`](docs/setup.md): adaptation, installation, testing and recovery.

Disk images, ROMs, ISOs, firmware variable stores, TPM state, credentials, logs, and the Windows device instance ID are deliberately not included. Obtain GPU firmware appropriate to your actual hardware; names alone do not establish compatibility.

## CPU choices

On this machine, CPUs `6–11,18–23` share one L3 cache. Consecutive guest sibling threads map to physical sibling pairs `6/18`, `7/19`, `8/20`, `9/21`, `10/22`, `11/23`. Emulator and IOThread affinity is `0–5,12–17`.

This is affinity, not exclusive CPU isolation: Linux tasks can still run on the guest-pinned CPUs. Verify your topology with `lscpu -e=CPU,CORE,SOCKET,CACHE` before copying it. Do not use 12 cores × 2 threads with only 12 vCPUs.

The latest example sets `host-passthrough`, `migratable=off` and requires `topoext`. These final CPU refinements passed XML validation but were not separately boot-tested after being added. Earlier 6-core/12-thread pinning, VirtIO storage/network and graceful GPU reboot were tested. GPU-related hypervisor settings are retained from that working baseline.

## Reset workaround

The observed sequence was: AMD works after a host restart, but returns Code 43 after a guest restart. Manually disabling AMD before guest shutdown and enabling it after startup avoided the failure.

The service automates that sequence:

1. Record the exact present Raphael GPU instance during installation.
2. On Windows `PRESHUTDOWN`, disable that GPU and verify problem code 22 (disabled).
3. On service startup, enable it and verify problem code 0.
4. Log outcomes in `C:\Program Files\EkremAmdResetGuard\guard.log`.

QXL is not required or automatically enabled. Looking Glass/physical output can go dark while the GPU is disabled. Ordinary service stop does not disable the GPU. Forced power-off, QEMU Reset, guest crashes, and host failures bypass this graceful mechanism. An already wedged GPU can still require a host restart.

## References

- [libvirt domain XML](https://libvirt.org/formatdomain.html)
- [libvirt QEMU Guest Agent](https://wiki.libvirt.org/Qemu_guest_agent.html)
- [Looking Glass B7 documentation](https://looking-glass.io/docs/B7/)
- [Microsoft service control handling](https://learn.microsoft.com/en-us/windows/win32/services/service-control-handler-function)
- [Ryzen GPU passthrough reference](https://github.com/isc30/ryzen-gpu-passthrough-proxmox)

The scripts are provided as source for review. Publication does not imply universal compatibility; retain a working VM definition before testing.
