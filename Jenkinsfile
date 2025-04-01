pipeline {
    agent any
    
    parameters {
        // Thêm tham số cho nhánh Git
        string(
            name: 'GIT_BRANCH',
            defaultValue: 'main',
            description: 'Nhánh Git để build (ví dụ: main, develop, feature/xyz)'
        )
        choice(
            name: 'TARGET_PLATFORM',
            choices: ['Windows', 'Android', 'iOS', 'WebGL', 'macOS', 'Linux'],
            description: 'Nền tảng mục tiêu để build game'
        )
        choice(
            name: 'BUILD_TYPE',
            choices: ['Release', 'Debug', 'Development', 'Profile'],
            description: 'Loại build (Release/Debug/Development/Profile)'
        )
        string(
            name: 'BUNDLE_VERSION',
            defaultValue: '1.0.0',
            description: 'Phiên bản bundle (x.y.z)'
        )
        string(
            name: 'BUNDLE_IDENTIFIER',
            defaultValue: 'com.UnderCats.TileCat',
            description: 'Bundle identifier'
        )
        // Các tham số đặc biệt cho Android
        choice(
            name: 'ANDROID_BUILD_FORMAT',
            choices: ['apk', 'aab'],
            description: 'Định dạng build Android (APK/AAB)'
        )
        choice(
            name: 'ANDROID_SCRIPTING_BACKEND',
            choices: ['IL2CPP', 'Mono'],
            description: 'Backend scripting cho Android'
        )
        choice(
            name: 'ANDROID_TARGET_ARCHITECTURE',
            choices: ['ARMv7', 'ARM64', 'X86', 'X86_64', 'All'],
            description: 'Kiến trúc CPU đích cho Android'
        )
        // Tham số đặc biệt cho keystore Android
        booleanParam(
            name: 'USE_KEYSTORE',
            defaultValue: false,
            description: 'Sử dụng keystore cho Android build (ký APK/AAB)'
        )
        // Tham số đặc biệt cho iOS
        string(
            name: 'IOS_TEAM_ID',
            defaultValue: '',
            description: 'Apple Developer Team ID (cho iOS builds)'
        )
    }

    options {
        // Giảm thiểu lỗi đường dẫn dài
        disableConcurrentBuilds()
        timestamps()
    }
    
    // Chỉ định công cụ Git sẽ sử dụng
    tools {
        git 'Default Git'
    }
    
    environment {
        // Đường dẫn tới Unity Editor
        UNITY_PATH = "C:\\UnityHub\\Unity\\Editor\\2021.3.6f1\\Editor\\2023.2.20f1-x86_64\\Editor\\Unity.exe"
        
        // Thư mục làm việc trong workspace
        WORKSPACE = "${WORKSPACE}"
        
        // Thư mục output cho build
        BUILD_OUTPUT = "${WORKSPACE}\\Build"
        
        // Discord webhook URL - Sử dụng credential ID thay vì URL trực tiếp
        DISCORD_WEBHOOK = credentials('discord-webhook-url')
        DISCORD_CHANNEL_ID = credentials('discord-channel-id')
        DISCORD_BOT_TOKEN = credentials('discord-bot-token')
        
        // Tên game và thông tin build
        GAME_NAME = "TileCat"
        BUILD_VERSION = "${BUILD_NUMBER}"
        
        // Nhánh Git (lấy từ tham số)
        SELECTED_GIT_BRANCH = "${params.GIT_BRANCH}"
        
        // Truyền các tham số vào môi trường
        TARGET_PLATFORM = "${params.TARGET_PLATFORM}"
        BUILD_TYPE = "${params.BUILD_TYPE}"
        BUNDLE_VERSION = "${params.BUNDLE_VERSION}"
        BUNDLE_IDENTIFIER = "${params.BUNDLE_IDENTIFIER}"
        
        // Tham số Android
        ANDROID_BUILD_FORMAT = "${params.ANDROID_BUILD_FORMAT}"
        ANDROID_SCRIPTING_BACKEND = "${params.ANDROID_SCRIPTING_BACKEND}"
        ANDROID_TARGET_ARCHITECTURE = "${params.ANDROID_TARGET_ARCHITECTURE}"
        USE_KEYSTORE = "${params.USE_KEYSTORE}"
        
        // Tham số iOS
        IOS_TEAM_ID = "${params.IOS_TEAM_ID}"
        
        // Thông tin Keystore (chỉ sử dụng khi USE_KEYSTORE = true)
        KEYSTORE_PATH = "${WORKSPACE}\\keystore\\build-key.keystore"
        // Sử dụng credentials để bảo mật thông tin nhạy cảm
        KEYSTORE_PASSWORD = credentials('android-keystore-password')
        KEYSTORE_ALIAS = credentials('android-keystore-alias')
        KEYSTORE_ALIAS_PASSWORD = credentials('android-keystore-alias-password')
    }
    
    stages {
        stage('Setup Git') {
            steps {
                // Cấu hình Git để hỗ trợ đường dẫn dài
                bat "git config --system core.longpaths true"
                // Hiển thị phiên bản Git
                bat "git --version"
            }
        }
        
        stage('Checkout') {
            steps {
                // Xóa workspace trước khi checkout để tránh lỗi repository bị hỏng
                cleanWs()
                
                // Checkout code từ GitHub repository với cấu hình nâng cao và nhánh được chọn
                checkout([$class: 'GitSCM',
                    branches: [[name: "*/${params.GIT_BRANCH}"]], // Sử dụng nhánh từ tham số
                    extensions: [
                        [$class: 'CloneOption', depth: 1, noTags: false, reference: '', shallow: true, timeout: 60],
                        [$class: 'CheckoutOption', timeout: 60]
                    ],
                    userRemoteConfigs: [[
                        credentialsId: 'github-access', 
                        url: 'https://github.com/TeamAcMong/Template-Monster-Survival.git'
                    ]]
                ])
                
                // Hiển thị branch đang được build
                bat "echo Building branch: %SELECTED_GIT_BRANCH%"
                bat "echo Target Platform: %TARGET_PLATFORM%"
                bat "echo Build Type: %BUILD_TYPE%"
            }
        }
        
        stage('Clean Previous Build') {
            steps {
                // Xóa thư mục build cũ nếu tồn tại
                bat "IF EXIST \"${BUILD_OUTPUT}\" RMDIR /S /Q \"${BUILD_OUTPUT}\""
                bat "MKDIR \"${BUILD_OUTPUT}\""
            }
        }
        
        stage('Setup Keystore') {
            when {
                allOf {
                    expression { return params.TARGET_PLATFORM.toLowerCase() == 'android' }
                    expression { return params.USE_KEYSTORE == true }
                }
            }
            steps {
                script {
                    // Sử dụng withCredentials để quản lý an toàn
                    withCredentials([
                        file(credentialsId: 'android-keystore-file', variable: 'KEYSTORE_FILE'),
                        string(credentialsId: 'android-keystore-password', variable: 'KEYSTORE_PASSWORD'),
                        string(credentialsId: 'android-keystore-alias', variable: 'KEYSTORE_ALIAS'),
                        string(credentialsId: 'android-keystore-alias-password', variable: 'KEYSTORE_ALIAS_PASSWORD')
                    ]) {
                        // Tạo thư mục keystore
                        bat """
                        IF NOT EXIST "${WORKSPACE}\\keystore" MKDIR "${WORKSPACE}\\keystore"
                        """
                        
                        // Copy keystore 
                        bat """
                        copy "${KEYSTORE_FILE}" "${WORKSPACE}\\keystore\\build-key.keystore"
                        """
                        
                        // Tạo file keystore.properties bằng bat thay vì PowerShell
                        bat """
                        (
                        echo KEYSTORE_PATH=${WORKSPACE}\\keystore\\build-key.keystore
                        echo KEYSTORE_PASSWORD=${KEYSTORE_PASSWORD}
                        echo KEYSTORE_ALIAS=${KEYSTORE_ALIAS}
                        echo KEYSTORE_ALIAS_PASSWORD=${KEYSTORE_ALIAS_PASSWORD}
                        ) > "${WORKSPACE}\\keystore\\keystore.properties"
                        """
                        
                        // Kiểm tra file được tạo
                        bat """
                        echo Keystore file copied to: ${WORKSPACE}\\keystore\\build-key.keystore
                        type "${WORKSPACE}\\keystore\\keystore.properties"
                        """
                    }
                }
            }
        }

        stage('Check Unity CLI') {
            steps {
                script {
                    try {
                        bat """
                        echo Checking Unity CLI version
                        "${UNITY_PATH}" -quit -batchmode -version
                        """
                    } catch (Exception e) {
                        error "Unable to run Unity CLI. Check Unity path and installation."
                    }
                }
            }
        }
        
        stage('Setup KeystoreHelper Script') {
            when {
                allOf {
                    expression { return params.TARGET_PLATFORM.toLowerCase() == 'android' }
                    expression { return params.USE_KEYSTORE == true }
                }
            }

            steps {
                
                // Kiểm tra xem KeystoreHelper.cs đã tồn tại trong repo chưa
                script {
                    def keystoreHelperExists = fileExists("${WORKSPACE}\\Assets\\Editor\\KeystoreHelper.cs")
                    if (!keystoreHelperExists) {
                        // Tạo thư mục Editor nếu không tồn tại
                        bat "IF NOT EXIST \"${WORKSPACE}\\Assets\\Editor\" MKDIR \"${WORKSPACE}\\Assets\\Editor\""
                        error "KeystoreHelper.cs script not found in repository. Please add the build script at Assets/Editor/KeystoreHelper.cs"
                    } else {
                        echo "KeystoreHelper.cs script found in repository."
                    }
                }
            }
        }
        
        stage('Check Builder Script') {
            steps {
                // Kiểm tra xem Builder.cs đã tồn tại trong repo chưa
                script {
                    def builderExists = fileExists("${WORKSPACE}\\Assets\\Editor\\Builder.cs")
                    if (!builderExists) {
                        // Tạo thư mục Editor nếu không tồn tại
                        bat "IF NOT EXIST \"${WORKSPACE}\\Assets\\Editor\" MKDIR \"${WORKSPACE}\\Assets\\Editor\""
                        error "Builder.cs script not found in repository. Please add the build script at Assets/Editor/Builder.cs"
                    } else {
                        echo "Builder.cs script found in repository."
                    }
                }
            }
        }
        
        stage('Unity Build') {
            steps {
                script {
                    // Sử dụng withEnv để đảm bảo các biến môi trường được truyền đúng
                    withEnv([
                        "UNITY_PATH=${UNITY_PATH}",
                        "WORKSPACE=${WORKSPACE}",
                        "TARGET_PLATFORM=${params.TARGET_PLATFORM}",
                        "BUILD_TYPE=${params.BUILD_TYPE}"
                    ]) {
                        // Chạy Unity build với lệnh verbose
                        def buildResult = bat(
                            script: """
                            echo Starting Unity Build
                            echo Unity Path: %UNITY_PATH%
                            echo Workspace: %WORKSPACE%
                            echo Target Platform: %TARGET_PLATFORM%
                            echo Build Type: %BUILD_TYPE%
                            echo Android Build Configuration:
                            echo Target Platform: %TARGET_PLATFORM%
                            echo Build Format: %ANDROID_BUILD_FORMAT%
                            echo Scripting Backend: %ANDROID_SCRIPTING_BACKEND%
                            echo Target Architecture: %ANDROID_TARGET_ARCHITECTURE%

                            \"${UNITY_PATH}\" -quit -batchmode -nographics -logFile - -projectPath \"${WORKSPACE}\" -executeMethod Builder.PerformBuild
                            """, 
                            returnStatus: true
                        )

                        // Ghi log build vào file để debug
                        bat """
                        \"${UNITY_PATH}\" -quit -batchmode -nographics -logFile \"${WORKSPACE}\\unity_verbose_build.log\" -projectPath \"${WORKSPACE}\" -executeMethod Builder.PerformBuild
                        """

                        // Hiển thị log verbose
                        bat "type \"${WORKSPACE}\\unity_verbose_build.log\""

                        // Kiểm tra kết quả build
                        if (buildResult != 0) {
                            error "Unity build failed. Check the logs for details."
                        }
                    }
                }
            }
            
            // Thêm post-build để luôn hiển thị log
            post {
                always {
                    script {
                        // Đảm bảo log luôn được hiển thị
                        if (fileExists("${WORKSPACE}\\unity_verbose_build.log")) {
                            echo "Unity Build Log:"
                            echo readFile("${WORKSPACE}\\unity_verbose_build.log")
                        } else {
                            echo "Unity verbose build log not found"
                        }
                    }
                }
            }
        }
        
        stage('Zip Build') {
                    steps {
                        script {
                            // Kiểm tra các biến môi trường
                    def buildOutput = env.BUILD_OUTPUT
                    def workspace = env.WORKSPACE
                    def gameName = env.GAME_NAME
                    def targetPlatform = env.TARGET_PLATFORM
                    def buildType = env.BUILD_TYPE
                    def gitBranch = env.GIT_BRANCH
                    def buildNumber = env.BUILD_NUMBER
        
                    // Xác định tên file build
                    def buildFileName
                    if (targetPlatform.toLowerCase() == 'android') {
                                buildFileName = env.ANDROID_BUILD_FORMAT.toLowerCase() == 'aab' ? 
                            "${gameName}_${targetPlatform}.aab" : 
                            "${gameName}_${targetPlatform}.apk"
                    } else if (targetPlatform.toLowerCase() == 'windows') {
                                buildFileName = "${gameName}_${targetPlatform}.exe"
                    } else {
                                buildFileName = "${gameName}_${targetPlatform}"
                    }
        
                    // Tạo tên file ZIP
                    def safeBranchName = gitBranch.replaceAll('[/\\\\]', '-')
                    def zipFileName = "${gameName}_${targetPlatform}_${buildType}_${safeBranchName}_${buildNumber}.zip"
        
                    // Thực hiện nén
                    bat """
                    @echo off
                    echo Build Output: ${buildOutput}
                    echo Build File: ${buildFileName}
                    echo ZIP Filename: ${zipFileName}
        
                    if not exist "${buildOutput}\\${buildFileName}" (
                        echo Build file not found: ${buildOutput}\\${buildFileName}
                        exit /b 1
                    )
        
                    powershell -Command "Compress-Archive -Path '${buildOutput}' -DestinationPath '${workspace}\\${zipFileName}'"
        
                    if not exist "${workspace}\\${zipFileName}" (
                        echo Failed to create ZIP file
                        exit /b 1
                    )
        
                    for %%A in ("${workspace}\\${zipFileName}") do (
                        set "SIZE=%%~zA"
                        set "SIZE_MB=!SIZE:~0,-6!"
                        echo Created ZIP file: ${zipFileName}
                        echo ZIP file size: !SIZE_MB! MB
                    )
                    """
                }
            }
        }
        
        stage('Deploy to Discord') {
                    steps {
                        script {
                            // Environment variables
                    def buildOutput = env.BUILD_OUTPUT
                    def workspace = env.WORKSPACE
                    def gameName = env.GAME_NAME
                    def targetPlatform = env.TARGET_PLATFORM
                    def buildType = env.BUILD_TYPE
                    def gitBranch = env.SELECTED_GIT_BRANCH ?: env.GIT_BRANCH
                    def buildNumber = env.BUILD_NUMBER
                    def bundleVersion = env.BUNDLE_VERSION
                    
                    // Create safe branch name for filename
                    def safeBranchName = gitBranch.replaceAll('[/\\\\]', '-')
                    
                    // Determine the build file name
                    def buildFileName
                    def zipFileName = "${gameName}_${targetPlatform}_${buildType}_${safeBranchName}_${buildNumber}.zip"
                    
                    // Log the deployment start
                    echo "Starting deployment to Discord..."
                    echo "File to deploy: ${workspace}\\${zipFileName}"
                    
                    // Create a project directory for Discord.NET
                    bat """
                    @echo off
                    
                    REM Check if .NET SDK is installed
                    where dotnet >nul 2>nul
                    if %errorlevel% neq 0 (
                        echo .NET SDK not installed. Please install .NET SDK.
                        exit /b 1
                    )
        
                    REM Create .NET project
                    mkdir DiscordUploader || echo Directory already exists
                    cd DiscordUploader
                    
                    REM Initialize project if not already initialized
                    if not exist "DiscordUploader.csproj" (
                        dotnet new console
                        dotnet add package Discord.Net
                    )
                    
                    REM Copy the DiscordUploader.cs file
                    copy ..\\DiscordUploader.cs Program.cs /Y
                    
                    REM Build the project
                    dotnet build -c Release
                    
                    REM Set environment variables for the Discord uploader
                    set DISCORD_BOT_TOKEN=${DISCORD_BOT_TOKEN}
                    set DISCORD_CHANNEL_ID=${DISCORD_CHANNEL_ID}
                    set TARGET_PLATFORM=${targetPlatform}
                    set BUILD_TYPE=${buildType}
                    set BUNDLE_VERSION=${bundleVersion}
                    set SELECTED_GIT_BRANCH=${gitBranch}
                    set BUILD_NUMBER=${buildNumber}
                    set GAME_NAME=${gameName}
                    
                    REM Run the Discord uploader
                    dotnet run --no-build -c Release "${workspace}\\${zipFileName}" "New build for ${gameName} (Build ${buildNumber})"
                    
                    REM Check if upload was successful
                    if %errorlevel% neq 0 (
                        echo Failed to upload build to Discord
                        exit /b 1
                    ) else (
                        echo Successfully uploaded build to Discord
                    )
                    """
                }
            }
            
            post {
                        success {
                            echo "Successfully deployed build to Discord with direct download link"
                }
                failure {
                            echo "Failed to deploy build to Discord"
                }
            }
        }
    }
    
    post {
        success {
            script {
                try {
                    // Thông báo khi build thành công
                    discordSend description: "Build #${BUILD_NUMBER} of ${env.GAME_NAME} successful! Platform: ${env.TARGET_PLATFORM}, Type: ${env.BUILD_TYPE}, Branch: ${env.SELECTED_GIT_BRANCH}", 
                                link: env.BUILD_URL, 
                                result: currentBuild.currentResult, 
                                title: "${env.GAME_NAME} Build Success", 
                                webhookURL: "${env.DISCORD_WEBHOOK}"
                } catch (Exception e) {
                    echo "Failed to send Discord notification: ${e.message}"
                }
            }
        }
        failure {
            script {
                try {
                    // Thông báo khi build thất bại
                    discordSend description: "Build #${BUILD_NUMBER} of ${env.GAME_NAME} failed! Platform: ${env.TARGET_PLATFORM}, Type: ${env.BUILD_TYPE}, Branch: ${env.SELECTED_GIT_BRANCH}", 
                                link: env.BUILD_URL, 
                                result: currentBuild.currentResult, 
                                title: "${env.GAME_NAME} Build Failed", 
                                webhookURL: "${env.DISCORD_WEBHOOK}"
                } catch (Exception e) {
                    echo "Failed to send Discord notification: ${e.message}"
                }
            }
        }
        always {
            script {
                try {
                    echo "Preserving build artifacts but cleaning up other files..."
                    
                    // Lưu file ZIP và log nếu tồn tại
                    if (fileExists("${WORKSPACE}\\${env.ZIP_FILENAME ?: ''}")) {
                        stash includes: "${env.ZIP_FILENAME}", name: 'build-zip', allowEmpty: true
                    }
                    
                    if (fileExists("${WORKSPACE}\\unity_build.log")) {
                        stash includes: "unity_build.log", name: 'build-log', allowEmpty: true
                    }
                    
                    // Dọn dẹp workspace một cách an toàn
                    cleanWs deleteDirs: true, disableDeferredWipeout: true, patterns: [
                        [pattern: 'Assets/**', type: 'INCLUDE'],
                        [pattern: 'Library/**', type: 'INCLUDE'],
                        [pattern: 'Logs/**', type: 'INCLUDE'],
                        [pattern: 'Packages/**', type: 'INCLUDE'],
                        [pattern: 'ProjectSettings/**', type: 'INCLUDE'],
                        [pattern: '.git/**', type: 'INCLUDE'],
                        [pattern: "${env.ZIP_FILENAME}", type: 'EXCLUDE'],
                        [pattern: "unity_build.log", type: 'EXCLUDE']
                    ]
                    
                    // Phục hồi file đã stash
                    unstash 'build-zip'
                    unstash 'build-log'
                    
                    echo "Workspace cleaned up successfully"
                } catch (Exception e) {
                    echo "Warning: Error during workspace cleanup: ${e.message}"
                }
            }
        }
    }
}