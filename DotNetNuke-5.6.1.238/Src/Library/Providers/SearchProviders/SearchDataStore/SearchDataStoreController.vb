'
' DotNetNuke® - http://www.dotnetnuke.com
' Copyright (c) 2002-2010
' by DotNetNuke Corporation
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'

Imports DotNetNuke.Data
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Services.Exceptions
Imports System.Collections.Generic
Imports DotNetNuke.Services.Tokens

Namespace DotNetNuke.Services.Search

    ''' -----------------------------------------------------------------------------
    ''' Namespace:  DotNetNuke.Services.Search
    ''' Project:    DotNetNuke.Search.DataStore
    ''' Class:      SearchDataStoreController
    ''' -----------------------------------------------------------------------------
    ''' <summary>
    ''' The SearchDataStoreController is the Business Controller class for SearchDataStore
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    '''		[cnurse]	11/15/2004	documented
    ''' </history>
    ''' -----------------------------------------------------------------------------
    Public Class SearchDataStoreController

        Public Shared Function AddSearchItem(ByVal item As SearchItemInfo) As Integer
            Return Data.DataProvider.Instance().AddSearchItem(item.Title, item.Description, item.Author, item.PubDate, item.ModuleId, item.SearchKey, item.GUID, item.ImageFileId)
        End Function

        Public Shared Sub DeleteSearchItem(ByVal SearchItemId As Integer)
            DataProvider.Instance().DeleteSearchItem(SearchItemId)
        End Sub

        Public Shared Sub DeleteSearchItemWords(ByVal SearchItemId As Integer)
            DataProvider.Instance().DeleteSearchItemWords(SearchItemId)
        End Sub

        Public Shared Function GetSearchItem(ByVal ModuleId As Integer, ByVal SearchKey As String) As SearchItemInfo
            Return CType(CBO.FillObject(DataProvider.Instance().GetSearchItem(ModuleId, SearchKey), GetType(SearchItemInfo)), SearchItemInfo)
        End Function

        Public Shared Function GetSearchItems(ByVal ModuleId As Integer) As Dictionary(Of String, SearchItemInfo)
            Return CBO.FillDictionary(Of String, SearchItemInfo)("SearchKey", DataProvider.Instance().GetSearchItems(Null.NullInteger, Null.NullInteger, ModuleId))
        End Function

        Public Shared Function GetSearchItems(ByVal PortalId As Integer, ByVal TabId As Integer, ByVal ModuleId As Integer) As ArrayList
            Return CBO.FillCollection(DataProvider.Instance().GetSearchItems(PortalId, TabId, ModuleId), GetType(SearchItemInfo))
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchResults gets the search results for a single word
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="PortalID">A Id of the Portal</param>
        ''' <param name="Word">The word</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Shared Function GetSearchResults(ByVal PortalID As Integer, ByVal Word As String) As SearchResultsInfoCollection
            Return Tokenize(New SearchResultsInfoCollection(CBO.FillCollection(DataProvider.Instance().GetSearchResults(PortalID, Word), GetType(SearchResultsInfo))))
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchResults gets the search results for a single word
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="PortalID">A Id of the Portal</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Shared Function GetSearchResults(ByVal PortalId As Integer, ByVal TabId As Integer, ByVal ModuleId As Integer) As SearchResultsInfoCollection
            Return Tokenize(New SearchResultsInfoCollection(CBO.FillCollection(DataProvider.Instance().GetSearchResults(PortalId, TabId, ModuleId), GetType(SearchResultsInfo))))
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchSettings gets the search settings for a single module
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="ModuleId">The Id of the Module</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Shared Function GetSearchSettings(ByVal ModuleId As Integer) As Dictionary(Of String, String)
            Dim dicSearchSettings As New Dictionary(Of String, String)

            Dim dr As IDataReader = Nothing
            Try
                dr = DataProvider.Instance().GetSearchSettings(ModuleId)
                While dr.Read()
                    If Not dr.IsDBNull(1) Then
                        dicSearchSettings(dr.GetString(0)) = dr.GetString(1)
                    Else
                        dicSearchSettings(dr.GetString(0)) = ""
                    End If
                End While
            Catch ex As Exception
                LogException(ex)
            Finally
                CBO.CloseDataReader(dr, True)
            End Try

            Return dicSearchSettings
        End Function

        Public Shared Sub UpdateSearchItem(ByVal item As SearchItemInfo)
            Data.DataProvider.Instance().UpdateSearchItem(item.SearchItemId, item.Title, item.Description, item.Author, item.PubDate, item.ModuleId, item.SearchKey, item.GUID, item.HitCount, item.ImageFileId)
        End Sub


        Public Shared Function Tokenize(ByRef searchResults As SearchResultsInfoCollection) As SearchResultsInfoCollection
            Dim i As Integer
            Dim tknReplace As New TokenReplace()
            For i = 0 To searchResults.Count - 1
                searchResults(i).Title = tknReplace.ReplaceEnvironmentTokens(searchResults(i).Title)
                searchResults(i).Description = tknReplace.ReplaceEnvironmentTokens(searchResults(i).Description)
            Next i
            Return searchResults
        End Function

    End Class

End Namespace