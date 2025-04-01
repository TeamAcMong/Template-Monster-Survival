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
        booleanParam(
            name: 'CLEAN_BUILD',
            defaultValue: false,
            description: 'Perform a clean build (true) or use cache from previous builds (false)'
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

        stage('Setup Ngrok URL') {
            steps {
                script {
                    try {
                        def ngrokUrl = bat(script: "@curl -s http://localhost:4040/api/tunnels | findstr /C:\"public_url\" | findstr /C:\"https\"", returnStdout: true).trim()
                        // Extract just the URL using regular expression
                        ngrokUrl = (ngrokUrl =~ /https:\/\/[^"]+/)[0]
                        env.NGROK_PUBLIC_URL = ngrokUrl
                        echo "Set NGROK_PUBLIC_URL to: ${ngrokUrl}"
                    } catch (Exception e) {
                        error "Failed to get ngrok URL: ${e.message}"
                    }
                }
            }
        }
        
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
                def gitBranch = env.SELECTED_GIT_BRANCH ?: env.GIT_BRANCH
                // Checkout code từ GitHub repository với cấu hình nâng cao và nhánh được chọn
                checkout([$class: 'GitSCM',
                    branches: [[name: "*/${gitBranch}"]], // Sử dụng nhánh từ tham số
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
                        script {
                            // Always clean the build output directory
                    bat "IF EXIST \"${BUILD_OUTPUT}\" RMDIR /S /Q \"${BUILD_OUTPUT}\""
                    bat "MKDIR \"${BUILD_OUTPUT}\""
                    
                    // If CLEAN_BUILD is true, also clean the Library folder to force a clean build
                    if (params.CLEAN_BUILD) {
                                echo "Performing clean build - removing Library folder and build cache"
                        bat """
                        IF EXIST \"${WORKSPACE}\\Library\" RMDIR /S /Q \"${WORKSPACE}\\Library\"
                        IF EXIST \"${WORKSPACE}\\Temp\" RMDIR /S /Q \"${WORKSPACE}\\Temp\"
                        """
                        // You might want to clean additional cache-related folders
                        bat """
                        IF EXIST \"${WORKSPACE}\\obj\" RMDIR /S /Q \"${WORKSPACE}\\obj\"
                        """
                    } else {
                                echo "Using cached build artifacts from previous builds"
                    }
                }
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
                            // Use withEnv to ensure all variables are properly passed to the build command
                    withEnv([
                        "UNITY_PATH=${UNITY_PATH}",
                        "WORKSPACE=${WORKSPACE}",
                        "TARGET_PLATFORM=${params.TARGET_PLATFORM}",
                        "BUILD_TYPE=${params.BUILD_TYPE}",
                        "CLEAN_BUILD=${params.CLEAN_BUILD}" // Pass the clean build parameter
                    ]) {
                                // Log build configuration
                        bat """
                        echo Starting Unity Build
                        echo Unity Path: %UNITY_PATH%
                        echo Workspace: %WORKSPACE%
                        echo Target Platform: %TARGET_PLATFORM%
                        echo Build Type: %BUILD_TYPE%
                        echo Clean Build: %CLEAN_BUILD%
                        echo Android Build Configuration:
                        echo Target Platform: %TARGET_PLATFORM%
                        echo Build Format: %ANDROID_BUILD_FORMAT%
                        echo Scripting Backend: %ANDROID_SCRIPTING_BACKEND%
                        echo Target Architecture: %ANDROID_TARGET_ARCHITECTURE%
                        """
        
                        // Run Unity build command
                        def buildResult = bat(
                            script: """
                            \"${UNITY_PATH}\" -quit -batchmode -nographics -logFile - -projectPath \"${WORKSPACE}\" -executeMethod Builder.PerformBuild
                            """, 
                            returnStatus: true
                        )
        
                        // Generate detailed log
                        bat """
                        \"${UNITY_PATH}\" -quit -batchmode -nographics -logFile \"${WORKSPACE}\\unity_verbose_build.log\" -projectPath \"${WORKSPACE}\" -executeMethod Builder.PerformBuild
                        """
        
                        // Display log
                        bat "type \"${WORKSPACE}\\unity_verbose_build.log\""
        
                        // Check build result
                        if (buildResult != 0) {
                                    error "Unity build failed. Check the logs for details."
                        }
                    }
                }
            }
            
            // Post-build log display remains the same
            post {
                        always {
                            script {
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
                    def gitBranch = env.SELECTED_GIT_BRANCH ?: env.GIT_BRANCH
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
                    
                    // Determine the zip file name
                    def zipFileName = "${gameName}_${targetPlatform}_${buildType}_${safeBranchName}_${buildNumber}.zip"
                    def fullZipPath = "${workspace}\\${zipFileName}"
                    
                    // Log the deployment start
                    echo "Starting Discord webhook notification..."
                    echo "Build file: ${fullZipPath}"
                    
                    // Get file size for the message
                    def fileSize = "Unknown"
                    try {
                                def fileSizeBytes = bat(script: "@for %%I in (\"${fullZipPath}\") do @echo %%~zI", returnStdout: true).trim()
                        def fileSizeMB = (fileSizeBytes.toLong() / (1024 * 1024)).round(2)
                        fileSize = "${fileSizeMB} MB"
                    } catch (Exception e) {
                                echo "Warning: Could not determine file size: ${e.message}"
                    }
                    
                    // Get ngrok public URL from environment or a config file
                    def ngrokUrl = env.NGROK_PUBLIC_URL
                    if (!ngrokUrl) {
                                echo "Warning: NGROK_PUBLIC_URL environment variable not set. Using fallback method to detect ngrok URL."
                        
                        // Try to get ngrok URL using curl (if running on the same machine)
                        try {
                                    ngrokUrl = bat(script: "@curl -s http://localhost:4040/api/tunnels | findstr /C:\"public_url\" | findstr /C:\"https\"", returnStdout: true).trim()
                            // Extract just the URL from the response using regular expression
                            ngrokUrl = (ngrokUrl =~ /https:\/\/[^"]+/)[0]
                        } catch (Exception e) {
                                    echo "Could not automatically detect ngrok URL: ${e.message}"
                            echo "Please set NGROK_PUBLIC_URL environment variable in Jenkins."
                            error "Failed to determine ngrok public URL"
                        }
                    }
                    
                    // Ensure ngrok URL doesn't end with a slash
                    if (ngrokUrl.endsWith('/')) {
                                ngrokUrl = ngrokUrl.substring(0, ngrokUrl.length() - 1)
                    }
                    
                    // Create the download URL - needs to be adjusted based on your Jenkins setup
                    // This assumes you have a file parameter plugin or some way to access the file
                    def downloadUrl = "${ngrokUrl}/job/${env.JOB_NAME}/${env.BUILD_NUMBER}/artifact/${zipFileName}"
                    
                    // Replace spaces with %20 for URL
                    downloadUrl = downloadUrl.replace(' ', '%20')
                    
                    // Create a Discord webhook payload with rich embed
                    def payload = """
                    {
                        "content": "New build available for ${gameName}!",
                        "embeds": [
                            {
                                "title": "${gameName} Build ${buildNumber}",
                                "description": "A new build has been generated with the following details:",
                                "color": 3447003,
                                "fields": [
                                    {
                                        "name": "Platform",
                                        "value": "${targetPlatform}",
                                        "inline": true
                                    },
                                    {
                                        "name": "Build Type",
                                        "value": "${buildType}",
                                        "inline": true
                                    },
                                    {
                                        "name": "Version",
                                        "value": "${bundleVersion ?: 'N/A'}",
                                        "inline": true
                                    },
                                    {
                                        "name": "Branch",
                                        "value": "${gitBranch}",
                                        "inline": true
                                    },
                                    {
                                        "name": "File Size",
                                        "value": "${fileSize}",
                                        "inline": true
                                    },
                                    {
                                        "name": "Download Link",
                                        "value": "[Click here to download](${downloadUrl})"
                                    }
                                ],
                                "footer": {
                                    "text": "Built on ${new Date().format("yyyy-MM-dd HH:mm:ss")}"
                                }
                            }
                        ]
                    }
                    """
                    
                    // Save payload to a temporary file
                    writeFile file: 'discord_payload.json', text: payload
                    
                    // Send the webhook using curl
                    withCredentials([string(credentialsId: 'discord-webhook-url', variable: 'DISCORD_WEBHOOK')]) {
                                def result = bat(script: """
                            curl -i -H "Accept: application/json" -H "Content-Type:application/json" -X POST --data-binary @discord_payload.json %DISCORD_WEBHOOK%
                        """, returnStatus: true)
                        
                        if (result != 0) {
                                    echo "Warning: Discord webhook notification failed"
                        } else {
                                    echo "Successfully sent Discord notification with download link"
                        }
                    }
                    
                    // Archive the artifact in Jenkins to make it available for download
                    archiveArtifacts artifacts: zipFileName, fingerprint: true
                }
            }
            
            post {
                        success {
                            echo "Successfully provided build download link via Discord"
                }
                failure {
                            echo "Failed to notify Discord about build"
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