'
' DotNetNuke® - http://www.dotnetnuke.com
' Copyright (c) 2002-2008 by DotNetNuke Corp. 
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

Imports DotNetNuke
Imports DotNetNuke.Entities.Host.HostSettings
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Entities.Portals
Imports DotNetNuke.Entities.Tabs
Imports DotNetNuke.Entities.Users
Imports DotNetNuke.Entities.Profile
Imports DotNetNuke.Entities.Host
Imports DotNetNuke.Services.Localization
Imports System.Globalization
Imports System.Web
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Reflection


Namespace DotNetNuke.Services.Tokens

    ''' <summary>
    ''' The TokenReplace class provides the option to replace tokens formatted 
    ''' [object:property] or [object:property|format] or [custom:no] within a string
    ''' with the appropriate current property/custom values.
    ''' Example for Newsletter: 'Dear [user:Displayname],' ==> 'Dear Superuser Account,'
    ''' Supported Token Sources: User, Host, Portal, Tab, Module, Membership, Profile, 
    '''                          Row, Date, Ticks, ArrayList (Custom), IDictionary
    ''' </summary>
    ''' <remarks></remarks>
    Public Class TokenReplace
        Inherits BaseCustomTokenReplace

#Region " Private Fields "

        Private _PortalSettings As PortalSettings
        Private _Hostsettings As Hashtable
        Private _ModuleInfo As Entities.Modules.ModuleInfo
        Private _User As Entities.Users.UserInfo
        Private _Tab As Entities.Tabs.TabInfo
        Private _ModuleId As Integer = Integer.MinValue

#End Region

#Region " Properties "

        ''' <summary>
        ''' Gets the Host settings from Portal
        ''' </summary>
        ''' <value>A hashtable with all settings</value>
        Private ReadOnly Property HostSettings() As Hashtable
            Get
                If _Hostsettings Is Nothing Then
                    _Hostsettings = GetSecureHostSettings()
                End If
                Return _Hostsettings
            End Get
        End Property


        ''' <summary>
        ''' Gets/sets the portal settings object to use for 'Portal:' token replacement
        ''' </summary>
        ''' <value>PortalSettings oject</value>
        Public Property PortalSettings() As PortalSettings
            Get
                Return _PortalSettings
            End Get
            Set(ByVal value As PortalSettings)
                _PortalSettings = value
            End Set
        End Property

        ''' <summary>
        ''' Gets/sets the module settings object to use for 'Module:' token replacement
        ''' </summary>
        Public Property ModuleInfo() As Entities.Modules.ModuleInfo
            Get
                If ModuleId > Integer.MinValue AndAlso (_ModuleInfo Is Nothing OrElse _ModuleInfo.ModuleID <> ModuleId) Then
                    Dim mc As New DotNetNuke.Entities.Modules.ModuleController
                    _ModuleInfo = mc.GetModule(ModuleId, PortalSettings.ActiveTab.TabID)
                End If
                Return _ModuleInfo
            End Get
            Set(ByVal value As Entities.Modules.ModuleInfo)
                _ModuleInfo = value
            End Set
        End Property

        ''' <summary>
        ''' Gets/sets the user object to use for 'User:' token replacement
        ''' </summary>
        ''' <value>UserInfo oject</value>
        Public Property User() As Entities.Users.UserInfo
            Get
                Return _User
            End Get
            Set(ByVal value As Entities.Users.UserInfo)
                _User = value
            End Set
        End Property



        ''' <summary>
        ''' Gets/sets the current ModuleID to be used for 'User:' token replacement
        ''' </summary>
        ''' <value>ModuleID (Integer)</value>
        Public Property ModuleId() As Integer
            Get
                Return _ModuleId
            End Get
            Set(ByVal value As Integer)

                _ModuleId = value
            End Set
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' creates a new TokenReplace object for default context
        ''' </summary>
        ''' <history>
        ''' 08/10/2007 sLeupold  documented
        ''' </history>
        Public Sub New()
            Me.New(Scope.DefaultSettings, Nothing, Nothing, Nothing, Null.NullInteger)
        End Sub

        ''' <summary>
        ''' creates a new TokenReplace object for default context and the current module
        ''' </summary>
        ''' <param name="ModuleID">ID of the current module</param>
        ''' <history>
        ''' 10/19/2007 sLeupold  added
        ''' </history>
        Public Sub New(ByVal ModuleID As Integer)
            Me.New(Scope.DefaultSettings, Nothing, Nothing, Nothing, ModuleID)
        End Sub

        ''' <summary>
        ''' creates a new TokenReplace object for custom context
        ''' </summary>
        ''' <param name="AccessLevel">Security level granted by the calling object</param>
        ''' <history>
        ''' 08/10/2007 sLeupold  documented
        ''' </history>
        Public Sub New(ByVal AccessLevel As Scope)
            Me.New(AccessLevel, Nothing, Nothing, Nothing, Null.NullInteger)
        End Sub

        ''' <summary>
        ''' creates a new TokenReplace object for custom context
        ''' </summary>
        ''' <param name="AccessLevel">Security level granted by the calling object</param>
        ''' <param name="ModuleID">ID of the current module</param>
        ''' <history>
        ''' 08/10/2007 sLeupold  documented
        ''' 10/19/2007 sLeupold  added
        ''' </history>
        Public Sub New(ByVal AccessLevel As Scope, ByVal ModuleID As Integer)
            Me.New(AccessLevel, Nothing, Nothing, Nothing, ModuleID)
        End Sub


        ''' <summary>
        ''' creates a new TokenReplace object for custom context
        ''' </summary>
        ''' <param name="AccessLevel">Security level granted by the calling object</param>
        ''' <param name="Language">Locale to be used for formatting etc.</param>
        ''' <param name="PortalSettings">PortalSettings to be used</param>
        ''' <param name="User">user, for which the properties shall be returned</param>
        ''' <history>
        ''' 08/10/2007 sLeupold  documented
        ''' </history>
        Public Sub New(ByVal AccessLevel As Scope, ByVal Language As String, ByVal PortalSettings As PortalSettings, ByVal User As UserInfo)
            Me.New(AccessLevel, Language, PortalSettings, User, Null.NullInteger)
        End Sub

        ''' <summary>
        ''' creates a new TokenReplace object for custom context
        ''' </summary>
        ''' <param name="AccessLevel">Security level granted by the calling object</param>
        ''' <param name="Language">Locale to be used for formatting etc.</param>
        ''' <param name="PortalSettings">PortalSettings to be used</param>
        ''' <param name="User">user, for which the properties shall be returned</param>
        ''' <param name="ModuleID">ID of the current module</param>
        ''' <history>
        '''     08/10/2007    sleupold  documented
        '''     10/19/2007    sleupold  ModuleID added
        ''' </history>
        Public Sub New(ByVal AccessLevel As Scope, ByVal Language As String, ByVal PortalSettings As PortalSettings, ByVal User As UserInfo, ByVal ModuleID As Integer)
            Me.CurrentAccessLevel = AccessLevel
            If AccessLevel <> Scope.NoSettings Then
                If PortalSettings Is Nothing Then
                    Me.PortalSettings = Entities.Portals.PortalController.GetCurrentPortalSettings
                Else
                    Me.PortalSettings = PortalSettings
                End If
                If User Is Nothing Then
                    Me.User = CType(HttpContext.Current.Items("UserInfo"), UserInfo)
                    Me.AccessingUser = Me.User
                Else
                    Me.User = User
                    Me.AccessingUser = CType(HttpContext.Current.Items("UserInfo"), UserInfo)
                End If
                If String.IsNullOrEmpty(Language) Then
                    Me.Language = New Localization.Localization().CurrentCulture
                Else
                    Me.Language = Language
                End If
                If ModuleID <> Null.NullInteger Then
                    Me.ModuleId = ModuleID
                End If
            End If
            PropertySource("date") = New DateTimePropertyAccess()
            PropertySource("datetime") = New DateTimePropertyAccess()
            PropertySource("ticks") = New TicksPropertyAccess()
            PropertySource("culture") = New CulturePropertyAccess()
        End Sub

#End Region

#Region "Public Replace Methods"

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <returns>string containing replaced values</returns>
        Public Function ReplaceEnvironmentTokens(ByVal strSourceText As String) As String
            If IsMyTokensInstalled() Then
                Return TokenizeWithMyTokens(strSourceText)
            End If
            Return ReplaceTokens(strSourceText)
        End Function

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <param name="row"></param>
        ''' <returns>string containing replaced values</returns>
        Public Function ReplaceEnvironmentTokens(ByVal strSourceText As String, ByVal row As DataRow) As String
            Dim rowProperties As New DataRowPropertyAccess(row)
            PropertySource("field") = rowProperties
            PropertySource("row") = rowProperties
            If IsMyTokensInstalled() Then
                Return TokenizeWithMyTokens(strSourceText)
            End If
            Return ReplaceTokens(strSourceText)
        End Function

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <param name="Custom"></param>
        ''' <param name="CustomCaption"></param>
        ''' <returns>string containing replaced values</returns>
        Public Function ReplaceEnvironmentTokens(ByVal strSourceText As String, ByVal Custom As ArrayList, ByVal CustomCaption As String) As String
            PropertySource.Add(CustomCaption.ToLower, New ArrayListPropertyAccess(Custom))
            If IsMyTokensInstalled() Then
                Return TokenizeWithMyTokens(strSourceText)
            End If
            Return ReplaceTokens(strSourceText)
        End Function

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <param name="Custom">NameValueList for replacing [custom:name] tokens, where 'custom' is specified in next param and name is either thekey or the index number in the string </param>
        ''' <param name="CustomCaption">Token name to be used inside token  [custom:name]</param>
        ''' <returns>string containing replaced values</returns>
        ''' <history>
        ''' 08/10/2007 sLeupold created
        ''' </history>
        Public Function ReplaceEnvironmentTokens(ByVal strSourceText As String, ByVal Custom As IDictionary, ByVal CustomCaption As String) As String
            PropertySource.Add(CustomCaption.ToLower, New DictionaryPropertyAccess(Custom))
            If IsMyTokensInstalled() Then
                Return TokenizeWithMyTokens(strSourceText)
            End If
            Return ReplaceTokens(strSourceText)
        End Function

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <param name="Custom">NameValueList for replacing [custom:name] tokens, where 'custom' is specified in next param and name is either thekey or the index number in the string </param>
        ''' <param name="CustomCaption">Token name to be used inside token  [custom:name]</param>
        ''' <param name="Row">DataRow, from which field values shall be used for replacement</param>
        ''' <returns>string containing replaced values</returns>
        ''' <history>
        ''' 08/10/2007 sLeupold created
        ''' </history>
        Public Function ReplaceEnvironmentTokens(ByVal strSourceText As String, ByVal Custom As ArrayList, ByVal CustomCaption As String, ByVal Row As System.Data.DataRow) As String
            Dim rowProperties As New DataRowPropertyAccess(Row)
            PropertySource("field") = rowProperties
            PropertySource("row") = rowProperties
            PropertySource.Add(CustomCaption.ToLower, New ArrayListPropertyAccess(Custom))
            If IsMyTokensInstalled() Then
                Return TokenizeWithMyTokens(strSourceText)
            End If
            Return ReplaceTokens(strSourceText)
        End Function

        ''' <summary>
        ''' Replaces tokens in strSourceText parameter with the property values, skipping environment objects
        ''' </summary>
        ''' <param name="strSourceText">String with [Object:Property] tokens</param>
        ''' <returns>string containing replaced values</returns>
        ''' <history>
        ''' 08/10/2007 sLeupold created
        ''' </history>
        Protected Overrides Function ReplaceTokens(ByVal strSourceText As String) As String
            InitializePropertySources()
            Return MyBase.ReplaceTokens(strSourceText)
        End Function


        Function IsMyTokensInstalled() As Boolean
            Dim cacheKey_Installed As String = "avt.MyTokens2.InstalledCore"

            If HttpRuntime.Cache.Get(cacheKey_Installed) Is Nothing Then
                TokenizeWithMyTokens(" ")
            End If
            Try
                Return HttpRuntime.Cache.Get(cacheKey_Installed).ToString() = "yes"
            Catch ex As Exception
                Return False
            End Try
        End Function


        Function TokenizeWithMyTokens(ByVal strContent As String) As String

            SyncLock GetType(TokenReplace)

                If HttpRuntime.Cache Is Nothing Then
                    Return strContent
                End If

                Dim cacheKey_Installed As String = "avt.MyTokens2.InstalledCore"
                Dim cacheKey_MethodReplaceWithProp As String = "avt.MyTokens2.MethodReplaceWithPropsCore"

                Dim bMyTokensInstalled As String = "no"
                Dim methodReplaceWithProps As MethodInfo = Nothing

                ' first, determine if MyTokens is installed
                Dim bCheck As Boolean = HttpRuntime.Cache.Get(cacheKey_Installed) Is Nothing
                If Not bCheck Then
                    bCheck = HttpRuntime.Cache.Get(cacheKey_Installed).ToString() = "yes" And HttpRuntime.Cache.Get(cacheKey_MethodReplaceWithProp) Is Nothing
                End If

                If bCheck Then

                    ' it's not in cache, let's determine if it's installed
                    Try
                        Dim myTokensRepl As Type = DotNetNuke.Framework.Reflection.CreateType("avt.MyTokens.MyTokensReplacer")
                        If myTokensRepl Is Nothing Then
                            Throw New Exception()
                        End If
                        ' handled in catch
                        bMyTokensInstalled = "yes"

                        ' we now know MyTokens is installed, get ReplaceTokensAll methods
                        methodReplaceWithProps = myTokensRepl.GetMethod( _
                            "ReplaceTokensAll", _
                            BindingFlags.[Public] Or BindingFlags.[Static], _
                            Nothing, _
                            CallingConventions.Any, _
                            New Type() { _
                                GetType(String), _
                                GetType(UserInfo), _
                                GetType(Boolean), _
                                GetType(ModuleInfo), _
                                GetType(System.Collections.Generic.Dictionary(Of String, IPropertyAccess)), _
                                GetType(Scope), _
                                GetType(UserInfo) _
                            }, Nothing)

                        If methodReplaceWithProps Is Nothing Then
                            ' this shouldn't really happen, we know MyTokens is installed
                            Throw New Exception()

                        End If
                    Catch
                        bMyTokensInstalled = "no"
                    End Try

                    ' cache values so next time the funciton is called the reflection logic is skipped
                    HttpRuntime.Cache.Insert(cacheKey_Installed, bMyTokensInstalled)
                    If bMyTokensInstalled = "yes" Then
                        HttpRuntime.Cache.Insert(cacheKey_MethodReplaceWithProp, methodReplaceWithProps)
                        HttpRuntime.Cache.Insert("avt.MyTokens.CorePatched", "true")
                        HttpRuntime.Cache.Insert("avt.MyTokens2.CorePatched", "true")
                    Else
                        HttpRuntime.Cache.Insert("avt.MyTokens.CorePatched", "false")
                        HttpRuntime.Cache.Insert("avt.MyTokens2.CorePatched", "false")
                    End If
                End If

                bMyTokensInstalled = HttpRuntime.Cache.Get(cacheKey_Installed).ToString()
                If bMyTokensInstalled = "yes" Then
                    If strContent.IndexOf("[") = -1 Then
                        Return strContent
                    End If
                    methodReplaceWithProps = DirectCast(HttpRuntime.Cache.Get(cacheKey_MethodReplaceWithProp), MethodInfo)
                    If (methodReplaceWithProps Is Nothing) Then
                        HttpRuntime.Cache.Remove(cacheKey_Installed)
                        Return TokenizeWithMyTokens(strContent)
                    End If
                Else
                    Return strContent
                End If

                ' we have MyTokens installed, proceed to token replacement
                Return DirectCast(methodReplaceWithProps.Invoke(Nothing, New Object() {strContent, User, Not (PortalController.GetCurrentPortalSettings().UserMode = PortalSettings.Mode.View), ModuleInfo, PropertySource, CurrentAccessLevel, AccessingUser}), String)

            End SyncLock
        End Function


#End Region

#Region "Private methods"

        ''' <summary>
        ''' setup context by creating appropriate objects
        ''' </summary>
        ''' <history>
        ''' /08/10/2007 sCullmann created
        ''' </history>
        ''' <remarks >
        ''' security is not the purpose of the initialization, this is in the resonsibilty of each property access class
        ''' </remarks>
        Private Sub InitializePropertySources()

            ' cleanup
            PropertySource.Remove("portal")
            PropertySource.Remove("tab")
            PropertySource.Remove("user")
            PropertySource.Remove("membership")
            PropertySource.Remove("profile")
            PropertySource.Remove("host")
            PropertySource.Remove("module")

            'initialization
            If CurrentAccessLevel >= Scope.Configuration Then
                PropertySource("portal") = PortalSettings
                PropertySource("tab") = PortalSettings.ActiveTab
                PropertySource("host") = New HostPropertyAccess()
                If Not (ModuleInfo Is Nothing) Then PropertySource("module") = ModuleInfo
            End If

            If CurrentAccessLevel >= Scope.DefaultSettings AndAlso Not (User Is Nothing OrElse User.UserID = -1) Then
                PropertySource("user") = User
                PropertySource("membership") = New MembershipPropertyAccess(User)
                PropertySource("profile") = New ProfilePropertyAccess(User)
            End If
        End Sub

#End Region

    End Class

End Namespace
