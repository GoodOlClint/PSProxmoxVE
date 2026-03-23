---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveClusterResource

## SYNOPSIS
Lists resources across the Proxmox VE cluster.

## SYNTAX

```
Get-PveClusterResource [[-Type] <String>] [-Node <String>] [-Session <PveSession>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Returns cluster-wide resources (VMs, containers, nodes, storage, SDN). Optionally filter by resource type or node name.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveClusterResource
```

Returns all cluster resources.

### Example 2
```powershell
PS C:\> Get-PveClusterResource -Type vm
```

Returns only VM resources across the cluster.

## PARAMETERS

### -Type
Filter by resource type.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: vm, lxc, node, storage, sdn

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Node
Filter results to a specific node name.

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

### PSProxmoxVE.Core.Models.Cluster.PveClusterResource
## NOTES

## RELATED LINKS
