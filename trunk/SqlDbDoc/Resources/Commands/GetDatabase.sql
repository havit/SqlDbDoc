SELECT 
	name		AS [name], 
	create_date	AS [dateCreated]
FROM
	sys.databases
WHERE 
	database_id = DB_ID()