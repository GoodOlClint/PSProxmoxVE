---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# New-PveContainerTemplate

## SYNOPSIS
Converts an LXC container to a template.

## SYNTAX

```
New-PveContainerTemplate -Node <String> -VmId <Int32> [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Converts the specified LXC container into a template via the Proxmox VE API. This operation is irreversible -- once converted, the container cannot be started and can only be used as a base for cloning.

## EXAMPLES

### Example 1
```powershell
PS C:\> New-PveContainerTemplate -Node "pve1" -VmId 100
```

Converts container 100 to a template.

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
Position: Named
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

### -VmId
The container identifier.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
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

### System.String
### System.Int32
## OUTPUTS

### PSProxmoxVE.Core.Models.Vms.PveTask
## NOTES

## RELATED LINKS
