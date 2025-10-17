-- Fix NULL or empty GUIDs
UPDATE Messages
SET MessageGuid = NEWID()
WHERE MessageGuid IS NULL OR MessageGuid = '';

-- Fix duplicates by assigning new GUIDs except first row
WITH DuplicateMessages AS (
  SELECT Id, MessageGuid,
         ROW_NUMBER() OVER (PARTITION BY MessageGuid ORDER BY Id) AS rn
  FROM Messages
)
UPDATE M
SET MessageGuid = NEWID()
FROM Messages M
JOIN DuplicateMessages D ON M.Id = D.Id
WHERE D.rn > 1;
