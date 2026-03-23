---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveBackupJob

## SYNOPSIS
Lists Proxmox VE backup jobs.

## SYNTAX

```
Get-PveBackupJob [[-Id] <String>] [-Session <PveSession>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Returns scheduled backup job configurations from the Proxmox VE cluster. Optionally filter by job ID.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveBackupJob
```

Returns all backup jobs.

### Example 2
```powershell
PS C:\> Get-PveBackupJob -Id "backup-abc123"
```

Returns the backup job with the specified ID.

## PARAMETERS

### -Id
The backup job ID to retrieve. When omitted, all jobs are returned.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
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

### PSProxmoxVE.Core.Models.Backup.PveBackupJob
## NOTES

## RELATED LINKS
