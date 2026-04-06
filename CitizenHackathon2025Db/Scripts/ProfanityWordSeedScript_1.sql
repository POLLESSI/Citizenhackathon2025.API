SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ProfanityWord', N'U') IS NULL
BEGIN
    RAISERROR('Table dbo.ProfanityWord not found.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

    WITH SeedData AS
    (
        SELECT *
        FROM (VALUES
            (N'Breeder', N'breeder', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Cishet', N'cishet', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Cissy', N'cissy', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Straights', N'straights', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Les Hets', N'les hets', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Hétéro', N'hétéro', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Hetero', N'hetero', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Homophobe', N'homophobe', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Homophobe', N'homophobe', N'fr', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Gayophobe', N'gayophobe', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Fragile', N'fragile', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Insécurisé', N'insécurisé', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'refoulé', N'refoulé', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'hétéro de base', N'hétéro de base', N'fr/en', 14, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'normie', N'normie', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Salaud', N'salaud', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Salaud de base', N'salaud de base', N'fr/en', 14, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Machiste', N'machiste', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Macho', N'macho', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Pauvre type', N'pauvre type', N'fr/en', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Abruti', N'abruti', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Dégénéré', N'dégénéré', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'privilège masculin', N'privilège masculin', N'fr/en', 18, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Cis-het', N'cis het', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Mascu', N'mascu', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Man-child', N'man child', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Mansplaining', N'mansplaining', N'fr/en', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Manterrupting', N'manterrupting', N'fr/en', 13, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'mecplication', N'mecplication', N'fr/en', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Pute', N'pute', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Sale pute', N'sale pute', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Fag hag', N'fag hag', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'pédale hag', N'pédale hag', N'fr/en', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Butch', N'butch', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bull dyke', N'bull dyke', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Carpet muncher', N'carpet muncher', N'fr/en', 14, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Rug muncher', N'rug muncher', N'fr/en', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'gougnotte', N'gougnotte', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'goudoune', N'goudoune', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'gousse', N'gousse', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'faggot', N'faggot', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'queer', N'queer', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'fairies', N'fairies', N'fr', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'gay', N'gay', N'fr', 3, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'transpédégouines', N'transpédégouines', N'fr', 16, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'nigga', N'nigga', N'fr', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'nègres', N'nègres', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'PD', N'pd', N'fr', 2, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'mauviettes', N'mauviettes', N'fr', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'orchidoclaste', N'orchidoclaste', N'fr', 13, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Dick', N'dick', N'fr:en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'trou du cul', N'trou du cul', N'fr:en', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Asshole', N'asshole', N'fr:en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Putain', N'putain', N'fr:en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Putain de garçon', N'putain de garçon', N'fr:en', 16, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Fuckboy', N'fuckboy', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Connard', N'connard', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'bite', N'bite', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'branleur', N'branleur', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'fluage', N'fluage', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Mr Balls of Cotton', N'mr balls of cotton', N'fr/en', 18, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'McLimpy le Grand Insatisfait', N'mclimpy le grand insatisfait', N'fr/en', 28, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'John Cena', N'john cena', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Sans bite', N'sans bite', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'puto', N'puto', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'fils de pute', N'fils de pute', N'fr/en', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bite molle', N'bite molle', N'fr/en', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Barbe de cou', N'barbe de cou', N'fr/en', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Letterkenny', N'letterkenny', N'fr/en', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Smelly', N'smelly', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'fiott', N'fiott', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'mauviette', N'mauviette', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'lesbienne', N'lesbienne', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'salope', N'salope', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'enculé', N'enculé', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Connasse', N'connasse', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Con', N'con', N'fr/en', 3, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'conne', N'conne', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'abruti', N'abruti', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'bouffon', N'bouffon', N'fr', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'salope', N'salope', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Fils de pute', N'fils de pute', N'fr', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'tocard', N'tocard', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Tu ne vaux rien', N'tu ne vaux rien', N'fr', 15, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Va te faire foutre !', N'va te faire foutre i', N'fr', 20, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Pauvre type', N'pauvre type', N'fr', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Crétin', N'crétin', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Crétine', N'crétine', N'fr', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Idiot', N'idiot', N'fr', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Idiote', N'idiote', N'fr', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Airbag Takata', N'airbag takata', N'fr', 13, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Féministe de merde va', N'féministe de merde va', N'fr', 21, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Mal baisée', N'mal baisée', N'fr', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Je te conchie', N'je te conchie', N'fr', 13, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Mortecouille', N'mortecouille', N'fr', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Sycophante', N'sycophante', N'fr', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Chiabrena', N'chiabrena', N'fr', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Arrière-Faix de Truie Larde', N'arrière faig de truie larde', N'fr', 27, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Flagorneur', N'flagorneur', N'fr', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Philistin', N'philistin', N'fr', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'chevaliers de la rosette', N'chevaliers de la rosette', N'fr', 24, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'coureuse de remparts', N'coureuse de remparts', N'fr', 20, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'travelo', N'travelo', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'tantouze', N'tantouze', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'tantouse', N'tantouse', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'slut', N'slut', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'whore', N'whore', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bullshit', N'bullshit', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Ass', N'ass', N'fr/en', 3, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Arse', N'arse', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Shut up', N'shut up', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Jerk', N'jerk', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Jerkass', N'jerkass', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Jerk off', N'jerk off', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Dumbass', N'dumbass', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Jackass', N'jackass', N'fr/en', 7, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Weirdo', N'weirdo', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Prick', N'prick', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Shit', N'shit', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Merde', N'merde', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'fait chier', N'fait chier', N'fr/en', 10, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Fuck you', N'fuck you', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Motherfucker', N'motherfucker', N'fr/en', 12, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Arsehole', N'arsehole', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Wanker', N'wanker', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Son of a bitch', N'son of a bitch', N'fr/en', 14, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bitch', N'bitch', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Dickhead', N'dickhead', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'moron', N'moron', N'fr/en', 5, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'nitwit', N'nitwit', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'doofus', N'doofus', N'fr/en', 6, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Smartass', N'smartass', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bonehead', N'bonehead', N'fr/en', 8, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'drama queen', N'drama queen', N'fr/en', 11, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'cock', N'cock', N'fr/en', 4, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'pédophile', N'pédophile', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'pedophile', N'pedophile', N'fr/en', 9, CAST(1 AS bit), N'Insult', CAST(1 AS bit)),
            (N'Bougnoul', N'bougnoul', N'fr/en', 8, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Bougnoule', N'bougnoule', N'fr/en', 9, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Négro', N'négro', N'fr/en', 5, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Négresse', N'négresse', N'fr/en', 8, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Fatma', N'fatma', N'fr/en', 5, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Feujs', N'feujs', N'fr/en', 5, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Youpin', N'youpin', N'fr/en', 6, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Sales français', N'sales français', N'fr/en', 15, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Sales belges', N'sales belges', N'fr/en', 12, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Sales wallons', N'sales wallons', N'fr/en', 13, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Sales flamands', N'sales flamands', N'fr/en', 14, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Frouze', N'frouze', N'fr/en', 6, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Camembert', N'camembert', N'fr/en', 9, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Flamouche', N'flamouche', N'fr/en', 9, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Flamouch', N'flamouch', N'fr/en', 8, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Vloems', N'vloems', N'fr/en', 6, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Boucaque', N'boucaque', N'fr/en', 8, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Khokhol', N'khokhol', N'fr/en', 7, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Moskal', N'moskal', N'fr/en', 6, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Pajeet', N'pajeet', N'fr/en', 6, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Rital', N'rital', N'fr/en', 5, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Smouglianki', N'smouglianki', N'fr/en', 11, CAST(1 AS bit), N'string', CAST(1 AS bit)),
            (N'Sac à frites', N'sac à frites', N'fr/en', 12, CAST(1 AS bit), N'string', CAST(1 AS bit))
        ) AS V
        (
            Word,
            NormalizedWord,
            LanguageCode,
            Weight,
            IsRegex,
            Category,
            Active
        )
    )
    MERGE dbo.ProfanityWord AS T
    USING SeedData AS S
        ON  T.Word = S.Word
        AND ISNULL(T.NormalizedWord, N'') = ISNULL(S.NormalizedWord, N'')
        AND ISNULL(T.LanguageCode, N'') = ISNULL(S.LanguageCode, N'')
    WHEN MATCHED THEN
        UPDATE SET
            T.Weight = S.Weight,
            T.IsRegex = S.IsRegex,
            T.Category = S.Category,
            T.Active = S.Active,
            T.NormalizedWord = S.NormalizedWord
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            Word,
            NormalizedWord,
            LanguageCode,
            Weight,
            IsRegex,
            Category,
            Active,
            CreatedAt
        )
        VALUES
        (
            S.Word,
            S.NormalizedWord,
            S.LanguageCode,
            S.Weight,
            S.IsRegex,
            S.Category,
            S.Active,
            SYSUTCDATETIME()
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO