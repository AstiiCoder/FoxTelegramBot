USE [CBTrade]
GO
/****** Object:  Trigger [dbo].[upd_PL_MESSAGE]    Script Date: 03.12.2020 14:38:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ========================================================================================
-- Author:		A.Tsvetkov
-- Create date: 05.11.2020
-- Description:	При вставке в эту таблицу происходит создание текстового файла с датой
-- ========================================================================================
ALTER TRIGGER [dbo].[upd_PL_MESSAGE]
   ON  [dbo].[PL_MESSAGE]
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;

	DECLARE  @cmd varchar (1024)
	SET  @cmd = 'bcp "SELECT GetDate()" queryout c:\signal.txt -c -C1251 -T' 
	exec xp_cmdshell @cmd 

end