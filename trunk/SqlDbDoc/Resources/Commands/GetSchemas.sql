SELECT DISTINCT
	S.name	AS [name]
FROM 
	sys.schemas AS S 
	LEFT JOIN sys.objects AS O ON S.schema_id = O.schema_id
WHERE 
	O.is_ms_shipped = 0 
	AND O.parent_object_id=0
ORDER BY S.name