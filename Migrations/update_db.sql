-- ALTER TABLE Blogs
--     ADD IsDeleted BIT NOT NULL DEFAULT 0,
--         DeletedAt DATETIME2 NULL;

ALTER TABLE Users
    ADD IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME2 NULL;