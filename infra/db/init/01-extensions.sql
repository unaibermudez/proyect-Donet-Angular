-- Habilita pgvector en la base de datos recién creada.
-- Se ejecuta automáticamente la primera vez que arranca el contenedor de Postgres.
-- La migración de EF Core también lo declara, así que esto es una red de seguridad
-- para que la base esté lista incluso antes de aplicar migraciones.
CREATE EXTENSION IF NOT EXISTS vector;
