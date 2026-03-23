---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveFirewallRef

## SYNOPSIS
Lists firewall references at the specified level.

## SYNTAX

```
Get-PveFirewallRef [-Level] <String> [-Node <String>] [-VmId <Int32>] [-Type <String>] [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Returns firewall references (aliases, IP sets, etc.) available at the Cluster, Node, Vm, or Container level. Optionally filter by type.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveFirewallRef -Level Cluster
```

Returns all cluster-level firewall references.

## PARAMETERS

### -Level
The firewall level: Cluster, Node, Vm, or Container.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: Cluster, Node, Vm, Container

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Node
The node name. Required when Level is Node, Vm, or Container.

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

### -Type
Optional type filter for references.

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

### -VmId
The VM/Container ID. Required when Level is Vm or Container.

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

### PSProxmoxVE.Core.Models.Firewall.PveFirewallRef
## NOTES

## RELATED LINKS
