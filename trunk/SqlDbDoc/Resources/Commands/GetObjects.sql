SELECT
	O.object_id		AS [id], 
	S.name			AS [schema], 
	O.name			AS [name], 
	O.type_desc		AS [type], 
	O.create_date	AS [dateCreated], 
	O.modify_date	AS [dateModified], 
	P.value			AS [description]
FROM 
	sys.objects AS O
	LEFT JOIN sys.schemas AS S on S.schema_id = o.schema_id
	LEFT JOIN sys.extended_properties AS P ON P.major_id = O.object_id AND P.minor_id = 0 and P.name = 'MS_Description' 
WHERE 
	is_ms_shipped = 0 
	AND parent_object_id = @parent_object_id

	

