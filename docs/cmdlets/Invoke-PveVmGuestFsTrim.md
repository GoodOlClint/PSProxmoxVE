---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Invoke-PveVmGuestFsTrim

## SYNOPSIS
Invokes an fstrim operation inside the guest via the QEMU guest agent.

## SYNTAX

```
Invoke-PveVmGuestFsTrim [-Node] <String> [-VmId] <Int32> [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Triggers an fstrim (filesystem trim) operation inside the guest operating system using the QEMU guest agent. This reclaims unused space on thin-provisioned storage. The guest agent must be installed and running inside the VM.

## EXAMPLES

### Example 1
```powershell
PS C:\> Invoke-PveVmGuestFsTrim -Node "pve1" -VmId 100
```

Triggers fstrim on VM 100.

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

### System.Int32
## OUTPUTS

### System.Void
## NOTES

## RELATED LINKS
