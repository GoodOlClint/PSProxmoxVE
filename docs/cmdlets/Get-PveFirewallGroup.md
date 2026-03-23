---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveFirewallGroup

## SYNOPSIS
Lists firewall security groups or rules within a group.

## SYNTAX

```
Get-PveFirewallGroup [[-Group] <String>] [-Session <PveSession>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Returns firewall security groups. When a group name is specified, returns the rules within that group.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveFirewallGroup
```

Returns all firewall security groups.

### Example 2
```powershell
PS C:\> Get-PveFirewallGroup -Group "webservers"
```

Returns the rules within the webservers security group.

## PARAMETERS

### -Group
The security group name. If specified, returns rules within the group.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
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

### PSProxmoxVE.Core.Models.Firewall.PveFirewallGroup
### PSProxmoxVE.Core.Models.Firewall.PveFirewallRule
## NOTES

## RELATED LINKS
