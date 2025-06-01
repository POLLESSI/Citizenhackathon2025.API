INSERT INTO [dbo].[CrowdInfo] ([LocationName], [Latitude], [Longitude], [CrowdLevel], [Timestamp], [Active])
VALUES 
('Place Bellecour', 45.7578, 4.8320, 2, GETDATE(), 1),
('Vieux Lyon', 45.7611, 4.8277, 4, GETDATE(), 1),
('Parc de la Tête d\''Or', 45.7797, 4.8556, 3, GETDATE(), 1),
('Confluence', 45.7412, 4.8156, 5, GETDATE(), 1),
('Part-Dieu', 45.7600, 4.8617, 1, GETDATE(), 1);