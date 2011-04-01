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

Imports System.Collections.Generic
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Entities.Host
Imports DotNetNuke.Entities.Portals
Imports DotNetNuke.Entities.Tabs
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Services.Exceptions
Imports DotNetNuke.Security.Permissions
Imports DotNetNuke.Security
Imports DotNetNuke.Services.Tokens


Namespace DotNetNuke.Services.Search

    ''' -----------------------------------------------------------------------------
    ''' Namespace:  DotNetNuke.Services.Search
    ''' Project:    DotNetNuke.Search.DataStore
    ''' Class:      SearchDataStore
    ''' -----------------------------------------------------------------------------
    ''' <summary>
    ''' The SearchDataStore is an implementation of the abstract SearchDataStoreProvider
    ''' class
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    '''		[cnurse]	11/15/2004	documented
    ''' </history>
    ''' -----------------------------------------------------------------------------
    Public Class SearchDataStore
        Inherits SearchDataStoreProvider

#Region "Private Methods"

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' AddIndexWords adds the Index Words to the Data Store
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="IndexId">The Id of the SearchItem</param>
        ''' <param name="SearchItem">The SearchItem</param>
        ''' <param name="Language">The Language of the current Item</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        '''     [cnurse]    11/16/2004  replaced calls to separate content clean-up
        '''                             functions with new call to HtmlUtils.Clean().
        '''                             replaced logic to determine whether word should
        '''                             be indexed by call to CanIndexWord()
        '''     [vnguyen]   09/03/2010  added searchitem title to the content and 
        '''                             also tab title, description, keywords where the 
        '''                             content resides for indexed searching
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Sub AddIndexWords(ByVal indexId As Integer, ByVal searchItem As SearchItemInfo, ByVal language As String)
            'Get the Search Settings for this Portal
            Dim settings As SearchConfig = New SearchConfig(SearchDataStoreController.GetSearchSettings(searchItem.ModuleId))

            Dim IndexWords As New Dictionary(Of String, Integer)
            Dim IndexPositions As New Dictionary(Of String, List(Of Integer))

            Dim Content As String = GetSearchContent(searchItem)
            Dim title As String = HtmlUtils.StripPunctuation(searchItem.Title, True)

            '' Tab and Module Metadata
            ' Retreive module and page names
            Dim objModule As ModuleInfo = New ModuleController().GetModule(searchItem.ModuleId)
            Dim objTab As TabInfo = New TabController().GetTab(objModule.TabID, objModule.PortalID, False)
            Dim tabName As String = HtmlUtils.StripPunctuation(objTab.TabName, True)
            Dim tabTitle As String = HtmlUtils.StripPunctuation(objTab.Title, True)
            Dim tabDescription As String = HtmlUtils.StripPunctuation(objTab.Description, True)
            Dim tabKeywords As String = HtmlUtils.StripPunctuation(objTab.KeyWords, True)
            Dim tagfilter As String = PortalController.GetPortalSetting("SearchIncludedTagInfoFilter", objModule.PortalID, Host.SearchIncludedTagInfoFilter)

            ' clean content
            Content = HtmlUtils.CleanWithTagInfo(Content, tagfilter, True)
            ' append tab and module metadata
            Content = Content.ToLower + title.ToLower + " " + tabName.ToLower + " " + tabTitle.ToLower + " " + tabDescription.ToLower + " " + tabKeywords.ToLower

            '' split content into words
            Dim ContentWords() As String = Split(Content, " ")

            ' process each word
            Dim intWord As Integer
            Dim strWord As String
            For Each strWord In ContentWords
                If CanIndexWord(strWord, language, settings) Then
                    intWord = intWord + 1
                    If IndexWords.ContainsKey(strWord) = False Then
                        IndexWords.Add(strWord, 0)
                        IndexPositions.Add(strWord, New List(Of Integer))
                    End If
                    ' track number of occurrences of word in content
                    IndexWords(strWord) = IndexWords(strWord) + 1
                    ' track positions of word in content
                    IndexPositions(strWord).Add(intWord)
                End If
            Next

            ' get list of words ( non-common )
            Dim Words As Hashtable = GetSearchWords()    ' this could be cached
            Dim WordId As Integer

            '' iterate through each indexed word
            Dim objWord As Object
            For Each objWord In IndexWords.Keys
                strWord = CType(objWord, String)
                If Words.ContainsKey(strWord) Then
                    ' word is in the DataStore
                    WordId = CType(Words(strWord), Integer)
                Else
                    ' add the word to the DataStore
                    WordId = Data.DataProvider.Instance().AddSearchWord(strWord)
                    Words.Add(strWord, WordId)
                End If
                ' add the indexword
                Dim SearchItemWordID As Integer = Data.DataProvider.Instance().AddSearchItemWord(indexId, WordId, IndexWords(strWord))
                Dim strPositions As String = Null.NullString
                For Each position As Integer In IndexPositions(strWord)
                    strPositions += position.ToString + ","
                Next
                Data.DataProvider.Instance().AddSearchItemWordPosition(SearchItemWordID, strPositions)
            Next

        End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' CanIndexWord determines whether the Word should be indexed
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="strWord">The Word to validate</param>
        ''' <returns>True if indexable, otherwise false</returns>
        ''' <history>
        '''		[cnurse]	11/16/2004	created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Function CanIndexWord(ByVal strWord As String, ByVal Locale As String, ByVal settings As SearchConfig) As Boolean
            'Create Boolean to hold return value
            Dim retValue As Boolean = True

            ' get common words for exclusion
            Dim CommonWords As Hashtable = GetCommonWords(Locale)

            'Determine if Word is actually a number
            If IsNumeric(strWord) Then
                'Word is Numeric
                If Not settings.SearchIncludeNumeric Then
                    retValue = False
                End If
            Else
                'Word is Non-Numeric

                'Determine if Word satisfies Minimum/Maximum length
                If strWord.Length < settings.SearchMinWordlLength OrElse strWord.Length > settings.SearchMaxWordlLength Then
                    retValue = False
                Else
                    'Determine if Word is a Common Word (and should be excluded)
                    If CommonWords.ContainsKey(strWord) = True AndAlso Not settings.SearchIncludeCommon Then
                        retValue = False
                    End If
                End If
            End If

            Return retValue
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetCommonWords gets a list of the Common Words for the locale
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="Locale">The locale string</param>
        ''' <returns>A hashtable of common words</returns>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Function GetCommonWords(ByVal Locale As String) As Hashtable
            Dim strCacheKey As String = "CommonWords" & Locale

            Dim objWords As Hashtable = CType(DataCache.GetCache(strCacheKey), Hashtable)
            If objWords Is Nothing Then
                objWords = New Hashtable
                Dim drWords As IDataReader = Data.DataProvider.Instance().GetSearchCommonWordsByLocale(Locale)
                Try
                    While drWords.Read
                        objWords.Add(drWords("CommonWord").ToString, drWords("CommonWord").ToString)
                    End While
                Finally
                    drWords.Close()
                    drWords.Dispose()
                End Try
                DataCache.SetCache(strCacheKey, objWords)
            End If
            Return objWords
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchWords gets a list of the current Words in the Database's Index
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <returns>A hashtable of words</returns>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Private Function GetSearchWords() As Hashtable
            Dim strCacheKey As String = "SearchWords"

            Dim objWords As Hashtable = CType(DataCache.GetCache(strCacheKey), Hashtable)
            If objWords Is Nothing Then
                objWords = New Hashtable
                Dim drWords As IDataReader = Data.DataProvider.Instance().GetSearchWords()
                Try
                    While drWords.Read
                        objWords.Add(drWords("Word").ToString, drWords("SearchWordsID"))
                    End While
                Finally
                    drWords.Close()
                    drWords.Dispose()
                End Try
                DataCache.SetCache(strCacheKey, objWords, TimeSpan.FromMinutes(2))
            End If
            Return objWords
        End Function

#End Region

#Region "Protected Methods"

        Protected Overridable Function GetSearchContent(ByVal SearchItem As SearchItemInfo) As String
            Return SearchItem.Content
        End Function

#End Region

#Region "Public Methods"

        'Public Overrides Sub AddSearchItem(ByVal SearchItem As SearchItemInfo)
        '    MyBase.AddSearchItem(SearchItem)
        'End Sub

        'Public Overrides Sub AddSearchItems(ByVal SearchItems As SearchItemInfoCollection)
        '    MyBase.AddSearchItems(SearchItems)
        'End Sub

        'Public Overrides Sub DeleteSearchItem(ByVal SearchItem As SearchItemInfo)
        '    MyBase.DeleteSearchItem(SearchItem)
        'End Sub

        'Public Overrides Sub DeleteSearchItems(ByVal SearchItems As SearchItemInfoCollection)
        '    MyBase.DeleteSearchItems(SearchItems)
        'End Sub

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchItems gets a collection of Search Items for a Module/Tab/Portal
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="PortalID">A Id of the Portal</param>
        ''' <param name="TabID">A Id of the Tab</param>
        ''' <param name="ModuleID">A Id of the Module</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Overrides Function GetSearchItems(ByVal PortalID As Integer, ByVal TabID As Integer, ByVal ModuleID As Integer) As SearchResultsInfoCollection
            Return SearchDataStoreController.GetSearchResults(PortalID, TabID, ModuleID)
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' GetSearchResults gets the search results for a passed in criteria string
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="PortalID">A Id of the Portal</param>
        ''' <param name="Criteria">The criteria string</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Overrides Function GetSearchResults(ByVal portalID As Integer, ByVal criteria As String) As SearchResultsInfoCollection
            Dim searchResult As SearchResultsInfo
            Dim criterion As SearchCriteria
            Dim hasExcluded As Boolean = Null.NullBoolean
            Dim hasMandatory As Boolean = Null.NullBoolean

            Dim objPortalController As New PortalController
            Dim objPortal As PortalInfo = objPortalController.GetPortal(portalID)

            'Get the Settings for this Portal
            Dim _PortalSettings As PortalSettings = New PortalSettings(objPortal)

            'We will assume that the content is in the locale of the Portal
            Dim commonWords As Hashtable = GetCommonWords(_PortalSettings.DefaultLanguage)

            ' clean criteria
            criteria = criteria.ToLower

            ' split search criteria into words
            Dim searchWords As New SearchCriteriaCollection(criteria)

            Dim searchResults As New Dictionary(Of String, SearchResultsInfoCollection)

            'dicResults is a Dictionary(Of SearchItemID, Dictionary(Of TabID, SearchResultsInfo)
            Dim dicResults As New Dictionary(Of Integer, Dictionary(Of Integer, SearchResultsInfo))

            ' iterate through search criteria words
            For Each criterion In searchWords
                If commonWords.ContainsKey(criterion.Criteria) = False OrElse _PortalSettings.SearchIncludeCommon Then
                    If Not searchResults.ContainsKey(criterion.Criteria) Then
                        searchResults.Add(criterion.Criteria, SearchDataStoreController.GetSearchResults(portalID, criterion.Criteria))
                    End If
                    If searchResults.ContainsKey(criterion.Criteria) Then
                        For Each result As SearchResultsInfo In searchResults(criterion.Criteria)
                            'Add results to dicResults
                            If Not criterion.MustExclude Then
                                If dicResults.ContainsKey(result.SearchItemID) Then
                                    'The Dictionary exists for this SearchItemID already so look in the TabId keyed Sub-Dictionary
                                    Dim dic As Dictionary(Of Integer, SearchResultsInfo) = dicResults.Item(result.SearchItemID)
                                    If dic.ContainsKey(result.TabId) Then
                                        'The sub-Dictionary contains the item already so update the relevance
                                        searchResult = dic.Item(result.TabId)
                                        searchResult.Relevance += result.Relevance
                                    Else
                                        'Add Entry to Sub-Dictionary
                                        dic.Add(result.TabId, result)
                                    End If
                                Else
                                    'Create new TabId keyed Dictionary
                                    Dim dic As New Dictionary(Of Integer, SearchResultsInfo)()
                                    dic.Add(result.TabId, result)

                                    'Add new Dictionary to SearchResults
                                    dicResults.Add(result.SearchItemID, dic)
                                End If
                            End If
                        Next
                    End If
                End If
            Next

            For Each criterion In searchWords
                Dim mandatoryResults As New Dictionary(Of Integer, Boolean)
                Dim excludedResults As New Dictionary(Of Integer, Boolean)
                If searchResults.ContainsKey(criterion.Criteria) Then
                    For Each result As SearchResultsInfo In searchResults(criterion.Criteria)
                        If criterion.MustInclude Then
                            'Add to mandatory results lookup
                            mandatoryResults.Item(result.SearchItemID) = True
                            hasMandatory = True
                        ElseIf criterion.MustExclude Then
                            'Add to exclude results lookup
                            excludedResults.Item(result.SearchItemID) = True
                            hasExcluded = True
                        End If
                    Next
                End If

                For Each kvpResults As KeyValuePair(Of Integer, Dictionary(Of Integer, SearchResultsInfo)) In dicResults
                    'The key of this collection is the SearchItemID,  Check if the value of this collection should be processed
                    If hasMandatory AndAlso (Not mandatoryResults.ContainsKey(kvpResults.Key)) Then
                        ' 1. If mandatoryResults exist then only process if in mandatoryResults Collection
                        For Each searchResult In kvpResults.Value.Values
                            searchResult.Delete = True
                        Next
                    ElseIf hasExcluded AndAlso (excludedResults.ContainsKey(kvpResults.Key)) Then
                        ' 2. Do not process results in the excludedResults Collection
                        For Each searchResult In kvpResults.Value.Values
                            searchResult.Delete = True
                        Next
                    End If
                Next
            Next

            'Process results against permissions and mandatory and excluded results
            Dim results As New SearchResultsInfoCollection
            Dim objTabController As New TabController
            Dim dicTabsAllowed As New Dictionary(Of Integer, Dictionary(Of Integer, Boolean))
            Dim tknReplace As New TokenReplace()

            For Each kvpResults As KeyValuePair(Of Integer, Dictionary(Of Integer, SearchResultsInfo)) In dicResults
                For Each searchResult In kvpResults.Value.Values
                    If Not searchResult.Delete Then
                        'Check If authorised to View Tab
                        Dim objTab As TabInfo = objTabController.GetTab(searchResult.TabId, portalID, False)
                        If TabPermissionController.CanViewPage(objTab) Then
                            'Check If authorised to View Module
                            Dim objModule As ModuleInfo = New ModuleController().GetModule(searchResult.ModuleId, searchResult.TabId, False)
                            If ModulePermissionController.CanViewModule(objModule) Then
                                searchResult.Title = tknReplace.ReplaceEnvironmentTokens(searchResult.Title)
                                searchResult.Description = tknReplace.ReplaceEnvironmentTokens(searchResult.Description)
                                results.Add(searchResult)
                            End If
                        End If
                    End If
                Next
            Next

            'Return Search Results Collection
            Return results
        End Function

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' StoreSearchItems adds the Search Item to the Data Store
        ''' </summary>
        ''' <remarks>
        ''' </remarks>
        ''' <param name="SearchItems">A Collection of SearchItems</param>
        ''' <history>
        '''		[cnurse]	11/15/2004	documented
        '''     [vnguyen]   09/07/2010  Modified: Added a date comparison for LastModifiedDate on the Tab
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Overrides Sub StoreSearchItems(ByVal SearchItems As SearchItemInfoCollection)
            'For now as we don't support Localized content - set the locale to the default locale. This
            'is to avoid the error in GetDefaultLanguageByModule which artificially limits the number
            'of modules that can be indexed.  This will need to be addressed when we support localized content.
            Dim Modules As New Dictionary(Of Integer, String)
            For Each item As SearchItemInfo In SearchItems
                If Not Modules.ContainsKey(item.ModuleId) Then
                    Modules.Add(item.ModuleId, "en-US")
                End If
            Next

            Dim objTabs As New TabController
            Dim objModule As New ModuleInfo
            Dim objTab As New TabInfo

            Dim searchItem As SearchItemInfo
            Dim indexedItems As Dictionary(Of String, SearchItemInfo)
            Dim moduleItems As SearchItemInfoCollection

            'Process the SearchItems by Module to reduce Database hits
            For Each kvp As KeyValuePair(Of Integer, String) In Modules
                'Get the Indexed Items that are in the Database for this Module
                indexedItems = SearchDataStoreController.GetSearchItems(kvp.Key)

                'Get the Module's SearchItems to compare
                moduleItems = SearchItems.ModuleItems(kvp.Key)

                'As we will be potentially removing items from the collection iterate backwards
                For iSearch As Integer = moduleItems.Count - 1 To 0 Step -1
                    searchItem = moduleItems(iSearch)

                    'Get item from Indexed collection
                    Dim indexedItem As SearchItemInfo = Nothing
                    If indexedItems.TryGetValue(searchItem.SearchKey, indexedItem) Then

                        'Get the tab where the search item resides -- used in date comparison
                        objModule = New ModuleController().GetModule(searchItem.ModuleId)
                        objTab = objTabs.GetTab(searchItem.TabId, objModule.PortalID, False)

                        'Item exists so compare Dates to see if modified
                        If indexedItem.PubDate < searchItem.PubDate OrElse indexedItem.PubDate < objModule.LastModifiedOnDate OrElse indexedItem.PubDate < objTab.LastModifiedOnDate Then
                            Try

                                If searchItem.PubDate < objModule.LastModifiedOnDate Then
                                    searchItem.PubDate = objModule.LastModifiedOnDate
                                End If
                                If searchItem.PubDate < objTab.LastModifiedOnDate Then
                                    searchItem.PubDate = objTab.LastModifiedOnDate
                                End If

                                'Content modified so update SearchItem and delete item's Words Collection
                                searchItem.SearchItemId = indexedItem.SearchItemId
                                SearchDataStoreController.UpdateSearchItem(searchItem)
                                SearchDataStoreController.DeleteSearchItemWords(searchItem.SearchItemId)

                                ' re-index the content
                                AddIndexWords(searchItem.SearchItemId, searchItem, kvp.Value)
                            Catch ex As Exception
                                'Log Exception
                                LogException(ex)
                            End Try
                        End If

                        'Remove Items from both collections
                        indexedItems.Remove(searchItem.SearchKey)
                        SearchItems.Remove(searchItem)
                    Else
                        Try
                            'Item doesn't exist so Add to Index
                            Dim indexID As Integer = SearchDataStoreController.AddSearchItem(searchItem)
                            ' index the content
                            AddIndexWords(indexID, searchItem, kvp.Value)
                        Catch ex As Exception
                            'Exception is probably a duplicate key error which is probably due to bad module data
                            LogSearchException(New SearchException(ex.Message, ex, searchItem))
                        End Try
                    End If
                Next
            Next
        End Sub

        'Public Overrides Sub UpdateSearchItem(ByVal SearchItem As SearchItemInfo)
        '    MyBase.UpdateSearchItem(SearchItem)
        'End Sub

        'Public Overrides Sub UpdateSearchItems(ByVal SearchItems As SearchItemInfoCollection)
        '    MyBase.UpdateSearchItems(SearchItems)
        'End Sub

#End Region

    End Class
End Namespace