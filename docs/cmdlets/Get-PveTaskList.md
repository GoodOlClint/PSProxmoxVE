---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveTaskList

## SYNOPSIS
Lists tasks on a Proxmox VE node.

## SYNTAX

```
Get-PveTaskList [-Node] <String> [-VmId <Int32>] [-Source <String>] [-TypeFilter <String>] [-Limit <Int32>]
 [-Session <PveSession>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Returns a list of recent tasks on the specified node. Use optional parameters to filter by VM ID, source, or task type. Unlike Get-PveTask which retrieves a single task by UPID, this cmdlet lists multiple tasks.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveTaskList -Node "pve1"
```

Returns the 50 most recent tasks on node pve1.

### Example 2
```powershell
PS C:\> Get-PveTaskList -Node "pve1" -VmId 100 -Limit 10
```

Returns the 10 most recent tasks for VM 100.

## PARAMETERS

### -Limit
Maximum number of tasks to return. Defaults to 50.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 50
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

### -Source
Filter by task source: all or active.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: all, active

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TypeFilter
Filter by task type (e.g., qmstart, vzdump).

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
Filter tasks by VM ID.

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

### System.String
## OUTPUTS

### PSProxmoxVE.Core.Models.Vms.PveTask
## NOTES

## RELATED LINKS
