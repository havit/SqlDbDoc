SELECT 
	C.name														AS [name],
	TYPE_NAME(C.system_type_id)									AS [type],
	C.max_length												AS [length],
	C.precision													AS [precision], 
	C.scale														AS [scale], 
	C.is_nullable												AS [nullable], 
	C.is_identity												AS [identity], 
	C.is_computed												AS [computed], 
	P.value														AS [description],
	PK.object_id												AS [primaryKey:refId],
	FK.constraint_object_id										AS [foreignKey:refId],
	FK.referenced_object_id										AS [foreignKey:tableId],
	COL_NAME(FK.referenced_object_id, FK.referenced_column_id)	AS [foreignKey:column],
	DF.object_id												AS [default:refId],
	DF.definition												AS [default:value]
FROM 
	sys.columns AS C
	LEFT JOIN sys.extended_properties P ON P.class = 1 AND P.major_id = C.object_id AND P.minor_id  = C.column_id AND P.name = 'MS_Description'
	LEFT JOIN sys.foreign_key_columns AS FK ON FK.parent_object_id = C.object_id AND FK.parent_column_id = C.column_id
	LEFT JOIN sys.default_constraints AS DF ON DF.parent_object_id = C.object_id AND DF.parent_column_id = C.column_id
	LEFT JOIN (
		SELECT
			PK.parent_object_id ,
			IC.column_id,
			PK.object_id 
		FROM 
			sys.key_constraints AS PK
			LEFT JOIN sys.index_columns AS IC on IC.object_id = PK.parent_object_id AND IC.index_id = PK.unique_index_id
		WHERE 
			PK.type='PK' 
	) AS PK ON PK.parent_object_id = C.object_id AND PK.column_id = C.column_id
WHERE C.object_id = @object_id
ORDER BY C.column_id