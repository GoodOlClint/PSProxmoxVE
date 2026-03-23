---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Import-PveOva

## SYNOPSIS
Imports an OVA file into a Proxmox VE virtual machine.

## SYNTAX

```
Import-PveOva [-Node] <String> [-Storage] <String> [-Path] <String> -TargetStorage <String> [-VmId <Int32>]
 [-Name <String>] [-Memory <Int32>] [-Cores <Int32>] [-Wait] [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Imports an OVA (Open Virtual Appliance) archive into Proxmox VE by parsing the embedded OVF descriptor, uploading the OVA, creating a new VM, and importing each disk. Use -Name, -Memory, or -Cores to override values from the OVF. Use -Wait to block until all import tasks complete.

## EXAMPLES

### Example 1
```powershell
PS C:\> Import-PveOva -Node "pve1" -Storage "local" -Path "C:\VMs\appliance.ova" -TargetStorage "local-lvm" -Wait
```

Imports an OVA file into a new VM on node pve1.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Cores
Override CPU core count.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Memory
Override memory size in MiB.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Override the VM name from the OVF.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Node
The PVE node name.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Local path to the OVA file.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
{{ Fill Session Description }}

```yaml
Type: PveSession
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Storage
The storage pool for OVA upload.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TargetStorage
Target storage for imported VM disks.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -VmId
The VM identifier. Auto-assigned if omitted.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wait
Wait for all tasks to complete before returning.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### PSProxmoxVE.Core.Models.Vms.PveVm
## NOTES

## RELATED LINKS
