---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveFirewallIpSetEntry

## SYNOPSIS
Lists entries in a firewall IP set.

## SYNTAX

```
Get-PveFirewallIpSetEntry [-Level] <String> [-Node <String>] [-VmId <Int32>] -Name <String>
 [-Session <PveSession>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Returns all entries (CIDR addresses) within the specified firewall IP set at the given level.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveFirewallIpSetEntry -Level Cluster -Name "trusted"
```

Returns all entries in the cluster-level IP set named trusted.

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

### -Name
The IP set name.

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

### None
## OUTPUTS

### PSProxmoxVE.Core.Models.Firewall.PveFirewallIpSetEntry
## NOTES

## RELATED LINKS
