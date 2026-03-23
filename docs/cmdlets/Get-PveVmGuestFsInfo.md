---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveVmGuestFsInfo

## SYNOPSIS
Gets filesystem information from the QEMU guest agent.

## SYNTAX

```
Get-PveVmGuestFsInfo [-Node] <String> [-VmId] <Int32> [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Queries the QEMU guest agent running inside the specified VM for its filesystem information, including mount points, filesystem types, and usage statistics. The guest agent must be installed and running inside the VM.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveVmGuestFsInfo -Node "pve1" -VmId 100
```

Returns filesystem information for VM 100.

## PARAMETERS

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

### -VmId
The VM identifier.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### System.Int32
## OUTPUTS

### PSProxmoxVE.Core.Models.Vms.PveGuestFsInfo
## NOTES

## RELATED LINKS
