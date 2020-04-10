<Serializable>
Public Class SeedFileInfo
    Property Name As String
    Property Extension As String
    Property FullPath As String
    Property length As ULong
    Property LastWrite As Date
    Property UserID As String
    Property Sonarr As Boolean
End Class

<Serializable>
Public Class SeedDirInfo
    Property Name As String
    Property FullPath As String
    Property LastWrite As Date
    Property UserID As String
    Property Sonarr As Boolean
End Class