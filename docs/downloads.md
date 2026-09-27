# Installation assets

The repository Releases page carries the reset guard source ZIP and Looking Glass B7 Windows installer, host archive and corresponding B7 source archive (including its LICENSE). Third-party files retain their upstream licenses.

- Looking Glass upstream: https://looking-glass.io/downloads
- VirtIO Windows 0.1.302 ISO: https://fedorapeople.org/groups/virt/virtio-win/direct-downloads/archive-virtio/virtio-win-0.1.302-1/virtio-win.iso
- Optional, not validated in this setup: https://github.com/VirtualDrivers/Virtual-Display-Driver/releases
- GPU firmware source: https://github.com/isc30/ryzen-gpu-passthrough-proxmox

Firmware redistribution permission was not established, so ROMs are referenced rather than mirrored. Required filenames are `vbios_7900x.bin` and `AMDGopDriver_7900.rom`; never use GitHub HTML pages saved as ROMs.

## Locally used files: SHA-256

These checksums identify the files used locally; they are not publisher signatures.

```text
303f7ae40dad495d6ae474fdc571df58958a4dbc5c37a522d80f9a203867949d  virtio-win-0.1.302.iso
fb77139ab8ea9ebda213a7a38fef873dda28f2ae73825f5dbf509b01de7c030d  vbios_7900x.bin
e7d26f895b15603f62a578dc624eb15c601d304b1dbae4eff4b95c817e9eedf3  AMDGopDriver_7900.rom
a701f2272e9fcf382849b24f913c6dd07597b3b1116525f2e90182f019609154  vdd-control.zip
```
