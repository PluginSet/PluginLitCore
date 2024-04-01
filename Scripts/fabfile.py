# -*- coding: utf-8 -*-
"""
fabfile
~~~~~~~~~~~~~~~~~~~

要使用 fab 命令，请在本地安装 Fabrile 2.1.x 及以上版本
"""

import os
import sys
import json
import logging
import hashlib
import zipfile
import shutil
import git
import plistlib
import datetime

from invoke import task
from invoke.exceptions import Exit

log = logging.Logger('fabric', level=logging.DEBUG)
log.addHandler(logging.StreamHandler(sys.stdout))


LogFiles = []

DRY_MODE = False

def dump_now(text):
    print(text, datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))


def SUCESS(tip):
    raise Exit(code=0, message="SUCCESS:%s" % tip)


def FAILURE(err, code=1, printLog=False):
    if printLog and len(LogFiles) > 0:
        file = LogFiles.pop()
        if os.path.exists(file):
            print('start print log file:: ', file)
            with open(file, 'r', encoding='UTF-8') as f:
                print("\n".join(f.readlines()))
            print('ended print log file:: ', file)
    raise Exit(code=code, message="FAILURE:%s" % err)


def get_unity_dict(filename):
    result = dict()
    with open(filename, 'r', encoding='UTF-8') as f:
        for line in f.readlines():
            kv = line.split(':')
            result[kv[0].strip()] = kv[1].strip()
    return result

def get_all_text(filename):
    with open(filename, 'r', encoding='UTF-8') as f:
        return f.read()

def get_file_first_line(filename):
    with open(filename, 'r', encoding='UTF-8') as f:
        return f.readline().strip()

def is_win_platform():
    return sys.platform.startswith('win')


# start init global params
WORK_PATH = os.getcwd()
PROTJECT_PATH = WORK_PATH
UNITY_HUB = os.environ.get("UNITY_HUB", None)
UNITY_PATH = os.environ.get("UNITY_PATH", None)
log.info('PROTJECT_PATH="%s"' % (PROTJECT_PATH))
log.info('UNITY_HUB="%s"' % (UNITY_HUB))
log.info('UNITY_PATH="%s"' % (UNITY_PATH))
if UNITY_HUB:
    unity_version = get_unity_dict(os.path.join(PROTJECT_PATH, "ProjectSettings", "ProjectVersion.txt"))
    log.info('unity_version="%s"' % (unity_version))
    path = os.path.join(UNITY_HUB, unity_version.get("m_EditorVersion", "unknow"))
    if os.path.exists(path):
        if is_win_platform():
            UNITY_PATH = os.path.join(path, "Editor", "Unity.exe")
        else:
            UNITY_PATH = os.path.join(path, "Unity.app", "Contents", "MacOS", "Unity")
        log.info("Set UNITY_PATH to hub version " + UNITY_PATH)
    else:
        log.info("Cannot find file with %s" % (path))

if not UNITY_PATH or not os.path.exists(UNITY_PATH):
    FAILURE("请先设置正确的UNITY_PATH或UNITY_HUB目录")

if is_win_platform():
    UNITY_PATH = '"%s"' % UNITY_PATH

def check_path(path):
    if os.path.exists(path):
        return True
    parent, _ = os.path.split(path)
    if check_path(parent):
        print('make dir ', path)
        os.mkdir(path)
        return True
    else:
        return FAILURE('cannot create path::' + path)


def execall(cmd):
    print('execall::: ', cmd)
    return os.system(cmd)


def rm_dir(dir):
    if os.path.exists(dir):
        shutil.rmtree(dir)


def rename_file(src, dst):
    if os.path.exists(src):
        os.rename(src, dst)


def rm_file(file):
    if os.path.exists(file):
        os.remove(file)


def file_md5(path):
    with open(path, 'rb') as f:
        sha = hashlib.md5(f.read()).hexdigest()
        return sha[0:6]


def copy_file(src, dst, md5=False):
    '''拷贝文件至目标文件，返回目标文件的全目录'''
    if not os.path.exists(src):
        return

    srcdir, srcname = os.path.split(src)
    dstdir, dstname = os.path.split(dst)
    check_path(dstdir)
    if md5:
        sha = file_md5(src)
        name_list = dstname.split('.')
        index_max = len(name_list) - 1
        new_name = '%s_%s.%s' % ('.'.join(name_list[0:index_max]), sha, name_list[index_max])
        dst = os.path.join(dstdir, new_name)
    shutil.copy(src, dst)
    return dst


def clear_ds_store(path):
    if os.path.isfile(path):
        if path.endswith('.DS_Store'):
            rm_file(path)
    else:
        for name in os.listdir(path):
            sub_path = os.path.join(path, name)
            if name == '.DS_Store':
                rm_file(sub_path)
            elif os.path.isdir(sub_path):
                clear_ds_store(sub_path)


def copy_path(src, dst, merge=False, md5=False, file_map=dict(), exclude_paths=set(), skip_files=set()):
    '''拷贝文件夹至目标文件夹中，如果merge为真则不会移除目录文件夹中多余的文件'''
    skip_files.add('.DS_Store')
    if not os.path.exists(src):
        print('not find path ', src)
        return
    check_path(dst)
    for name in os.listdir(src):
        from_path = os.path.join(src, name)
        to_path = os.path.join(dst, name)
        if os.path.isdir(from_path):
            if exclude_paths.__contains__(from_path):
                continue
            if not merge:
                rm_dir(to_path)
            copy_path(from_path, to_path, merge, md5, file_map, exclude_paths, skip_files)
        else:
            if exclude_paths.__contains__(from_path):
                continue
            if skip_files.__contains__(name):
                continue
            file_map[from_path] = copy_file(from_path, to_path, md5)


def copy_path_ex(src, dst, params, merge=False):
    '''拷贝文件夹至目标文件夹中，根据params修改文件名和文件内容，如果merge为真则不会移除目录文件夹中多余的文件'''
    if not os.path.exists(src):
        return
    for name in os.listdir(src):
        from_path = os.path.join(src, name)
        to_path = os.path.join(dst, name)
        if os.path.isdir(from_path):
            if not os.path.exists(to_path):
                os.mkdir(to_path)
            else:
                if not merge:
                    rm_dir(to_path)
            copy_path_ex(from_path, to_path, params, merge)
        else:
            with open(from_path, 'r', encoding='UTF-8') as f:
                try:
                    content = f.read()
                    if not content:
                        copy_file(from_path, to_path)
                    else:
                        with open(to_path, 'w', encoding='UTF-8') as w:
                            w.write(content)
                except Exception as identifier:
                    copy_file(from_path, to_path)


def parseJsonFile(path):
    '''解析JSON文件'''
    with open(path, 'r', encoding='UTF-8') as f:
        return json.load(f)
    return None


def resaveJsonFile(path):
    try:
        data = parseJsonFile(path)
        with open(path, 'w') as f:
            json.dump(data, f)
    except Exception as identifier:
        print(identifier)


def transformJsonDir(path):
    for name in os.listdir(path):
        sub_path = os.path.join(path, name)
        if os.path.isdir(sub_path):
            transformJsonDir(sub_path)
        else:
            resaveJsonFile(sub_path)


def encodeObjectParam(data):
    '''将字典转换为替换字符串'''
    result = str(data)
    result = result[1:len(result) - 1]
    li = result.split(',')
    return ',\n'.join(li)


def copy_file_with_alter(src_path, dst_path, alter_func, params):
    if not os.path.exists(src_path):
        return
    if not os.path.exists(dst_path):
        copy_file(src_path, dst_path)

    with open(src_path, 'r', encoding='UTF-8') as r:
        context = r.read()
        if alter_func:
            context = alter_func(context, params)

    if context:
        with open(dst_path, 'w', encoding='UTF-8') as w:
            w.write(context)
            print('write file %s success' % dst_path)


def get_all_path(path, result):
    for name in os.listdir(path):
        sub_path = os.path.join(path, name)
        if os.path.isdir(sub_path):
            get_all_path(sub_path, result)
        else:
            result.append(sub_path)


def zip_file(zip_path, zip_file):
    all_files = []
    get_all_path(zip_path, all_files)
    with zipfile.ZipFile(zip_file, 'w', zipfile.ZIP_DEFLATED) as f:
        for path in all_files:
            print('append zip file :: ', path)
            f.write(path, os.path.relpath(path, zip_path))
        f.close()

class ArchiveInfo(object):
    """<?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
            <key>ApplicationProperties</key>
            <dict>
                <key>ApplicationPath</key>
                <string>Applications/barrett.app</string>
                <key>CFBundleIdentifier</key>
                <string>com.sagi.barrett</string>
                <key>CFBundleShortVersionString</key>
                <string>1.0.7</string>
                <key>CFBundleVersion</key>
                <string>8</string>
                <key>SigningIdentity</key>
                <string>Apple Development: Haifeng Deng (WA6MJB2Y9P)</string>
                <key>Team</key>
                <string>5KRL6VS2Z2</string>
            </dict>
            <key>ArchiveVersion</key>
            <integer>2</integer>
            <key>CreationDate</key>
            <date>2020-01-06T16:19:57Z</date>
            <key>Name</key>
            <string>Unity-iPhone</string>
            <key>SchemeName</key>
            <string>Unity-iPhone</string>
        </dict>
        </plist>"""
    path_application = ""
    bundle_id = ""
    version_name = ""
    version_code = 0
    cert = ""
    team_id = ""
    scheme = ""

    def from_archive_path(self, archive_path):
        info_path = os.path.join(archive_path, "Info.plist")
        if not os.path.exists(info_path):
            return
        with open(info_path, "rb") as f:
            plist = plistlib.load(f)

        application_properties = plist.get("ApplicationProperties")
        self.scheme = plist.get("SchemeName")

        self.path_application = application_properties.get("ApplicationPath")
        self.bundle_id = application_properties.get("CFBundleIdentifier")
        self.version_name = application_properties.get("CFBundleShortVersionString")
        self.version_code = application_properties.get("CFBundleVersion")
        self.cert = application_properties.get("SigningIdentity")
        self.team_id = application_properties.get("Team")
    

class BuildConfig(object):
    CONFIG_JSON_PATH = "build_config.json"

    path_project = ""
    path_project_ios = ""
    path_info_plist = ""

    build_types = None

    build_type = ""
    team_id = ""
    scheme = ""
    bundle_id = ""
    sign_style = ""
    bite_code = False
    method = ""
    profile = ""
    cert = ""
    configuration = ""
    version_name = ""
    version_code = 0
    build_num = 0

    name_ipa = ""

    path_build = ""
    path_output = ""
    path_tmp = ""

    def set_config(self, project, build_type, configuration, version_name, version_code, build_num):
        path, _ = os.path.split(project)
        self.path_project = path
        self.path_project_ios = project
        self.build_type = build_type
        self.configuration = configuration
        self.version_name = version_name
        self.version_code = version_code
        self.build_num = build_num

        self.path_output = os.path.join(self.path_project, "build")
        self.read_from_file()
        self.name_ipa = "_".join([self.bundle_id, self.version_name, str(self.build_num)]) + ".ipa"
        

    def read_from_file(self):
        path_config = os.path.join(self.path_project, self.CONFIG_JSON_PATH)

        if not os.path.exists(path_config):
            print('没有配置文件:' + path_config)
            return False

        with open(path_config, 'r') as f:
            json_obj = json.load(f)
            f.close()

        self.read_from_json(json_obj)

    def read_from_json(self, json_obj):
        path_info = json_obj.get("info")
        scheme = json_obj.get("scheme")
        build_types = json_obj.get("buildTypes")
        print(build_types, self.build_type)
        build_type_obj = build_types.get(self.build_type)

        self.path_info_plist = os.path.join(self.path_project, path_info)
        self.scheme = scheme
        self.team_id = build_type_obj.get("teamId")
        self.bundle_id = build_type_obj.get("bundleId")
        self.method = build_type_obj.get("method")
        self.sign_style = build_type_obj.get("signStyle")
        self.profile = build_type_obj.get("profile")
        self.cert = build_type_obj.get("cert")
        self.bite_code = build_type_obj.get("biteCode")

    def get_derived_data_path(self):
        return os.path.join(self.path_output, "tmp", str(self.build_num))

    def get_export_path(self):
        return os.path.join(self.path_output, str(self.build_num))

    def get_export_plist_path(self):
        return os.path.join(self.path_output, "tmp", str(self.build_num), self.bundle_id)

    def get_method(self):
        return self.method

    def generate_system_archive_path(self):
        """
        获得系统的 archive 文件夹,这样使用命令行导出后,在 xcode organizer 窗口一样可以看到
        :return:
        """
        username = os.environ.get('USER')
        group_dir = "_".join([self.bundle_id, self.version_name])
        filename = "_".join([self.bundle_id, self.version_name, str(self.build_num)])
        archive_path = "/Users/{username}/Library/Developer/Xcode/Archives/{group_dir}/{filename}.xcarchive".format(**locals())
        return archive_path

    def __str__(self):
        values = []
        for name, value in vars(self).items():
            values.append('%s=%s' % (name, value))

        return "\n".join(values)


class BuildToolIOS(object):
    build_config = None
    quiet = False
    dry = False

    def __init__(self, build_config, quiet, dry):
        self.build_config = build_config
        self.quiet = quiet
        self.dry = dry

    def ios_archive(self):
        code_sign_identity = self.build_config.cert
        provisioning_profile = self.build_config.profile

        project_path = self.build_config.path_project_ios
        scheme = self.build_config.scheme
        archive_path = self.build_config.generate_system_archive_path()
        derived_data_path = self.build_config.get_derived_data_path()
        configuration = self.build_config.configuration

        if project_path.endswith(".xcworkspace"):
            cmds = [
                "xcodebuild",
                "archive",
                "-workspace", project_path,
                "-scheme", scheme,
                "-archivePath", archive_path,
                "-configuration", configuration,
                "-derivedDataPath", derived_data_path,
                "-allowProvisioningUpdates",
            ]
        else:
            cmds = [
                "xcodebuild",
                "archive",
                "-project", project_path,
                "-scheme", scheme,
                "-archivePath", archive_path,
                "-configuration", configuration,
                "-derivedDataPath", derived_data_path,
                "-allowProvisioningUpdates",
            ]

        if code_sign_identity and provisioning_profile:
            # cmds.append("CODE_SIGN_IDENTITY='{code_sign_identity}'".format(**locals()))
            cmds.append("PROVISIONING_PROFILE_APP={provisioning_profile}".format(**locals()))
            # cmds.append("CODE_SIGN_STYLE=Manual")

        cmd = " ".join(cmds)
        print(cmd)
        self.run_print_result(cmd)
        return archive_path

    def ios_archive_export(self, archive_path, archive_info, export_path, method):
        """
        导出 archive
        :return path_ipa
        """

        archive_path = archive_path or ""
        export_path = export_path or ""

        scheme = archive_info.scheme
        version_name = archive_info.version_name
        version_code = archive_info.version_code

        export_plist_path = self.create_export_plist(method)

        cmds = [
            "xcodebuild",
            "-exportArchive",
            "-archivePath", archive_path,
            "-exportPath", export_path,
            "-exportOptionsPlist", export_plist_path,
            "-allowProvisioningUpdates",
        ]
        cmd = " ".join(cmds)
        print(cmd)
        self.run_print_result(cmd)
        result_plist = os.path.join(export_path, 'DistributionSummary.plist')
        if not os.path.exists(result_plist):
            return os.path.join(export_path, scheme + ".ipa")

        with open(result_plist, "rb") as f:
            plist = plistlib.load(f)
        ipa_names = list(plist.keys())
        ipa_name = None
        if len(ipa_names) > 0:
            ipa_name = ipa_names[0]
        if ipa_name == None:
            ipa_name = scheme + ".ipa"
        return os.path.join(export_path, ipa_name)

    def create_export_plist(self, method):
        plist_path = self.build_config.get_export_plist_path()
        plist_file = os.path.join(plist_path, 'export.plist')
        plist_dict = {
            "teamID": self.build_config.team_id,
            "signingStyle": self.build_config.sign_style,
            "signingCertificate": self.build_config.cert,
            "provisioningProfiles": {
                self.build_config.bundle_id: self.build_config.profile
            },
            "method": method,
            "compileBitcode": self.build_config.bite_code
        }
        print(plist_dict)
        if os.path.exists(plist_file):
            os.remove(plist_file)

        check_path(plist_path)
        with open(plist_file, "wb+") as f:
            plistlib.dump(plist_dict, f)
            f.close()

        return plist_file

    def clean(self):
        if os.path.exists(self.ipaPath):
            os.remove(self.ipaPath)
        if os.path.exists(self.tmpPath):
            cmds = [
                "rm",
                "-rf",
                self.tmpPath,
            ]
            if self.__execute(cmds) == 0:
                print('Clean temp dir success')
        if os.path.exists(self.build_path):
            cmds = [
                "rm",
                "-rf",
                self.build_path,
            ]
            if self.__execute(cmds) == 0:
                print('Clean dirs success')

    def archive(self):
        self.__prepare()
        archivePath = self.savePath + '/' + self.scheme + '.xcarchive'
        if os.path.exists(archivePath):
            execall("rm -rf " + archivePath)

        print('use scheme: %s' % self.scheme)
        print('use configuration: %s' % self.configuration)
        cmds = [
            'xcodebuild',
            'archive',
            "-scheme", self.scheme,
            "-configuration", self.configuration,
            "-derivedDataPath", self.build_path,
            "-archivePath", archivePath,
        ]
        if self.workspaceFile:
            cmds.append('-workspace')
            cmds.append(self.workspaceFile)
        if len(self.provisioning_profile_uuid) > 0:
            cmds.append("PROVISIONING_PROFILE_SPECIFIER=" + self.provisioning_profile_uuid)
        if len(self.certification_name) > 0:
            cmds.append("CODE_SIGN_IDENTITY=" + self.certification_name)
        if self.team_id:
            cmds.append("DEVELOPMENT_TEAM=" + self.team_id)
        if len(self.provisioning_profile_uuid) > 0 or len(self.certification_name) > 0 or self.team_id:
            cmds.append("CODE_SIGN_STYLE=Manual")
        if self.__execute(cmds) == 0:
            print("Archive project success!")

    def run_cmd(self, cmd):
        """
        运行脚本
        :return:
        """
        if not self.dry:
            return os.popen(cmd)

    def run_get_result(self, cmd):
        pipe = self.run_cmd(cmd)
        return pipe.read()

    def run_print_result(self, cmd):
        print("run:" + cmd)
        pipe = self.run_cmd(cmd)

        if not self.dry:
            while pipe and True:
                try:
                    line = pipe.readline()
                    if line:
                        print(line)
                    else:
                        break
                except Exception as e:
                    print(str(e))
                    pass
                    
# common ended==============================

# buildTarget Allows the selection of an active build target before loading a project. Possible options are:
# # standalone,
# # Win,
# # Win64,
# # OSXUniversal,
# # Linux,
# # Linux64,
# # LinuxUniversal,
# iOS,
# Android,
# # Web,
# # WebStreamed,
# # WebGL,
# # XboxOne,
# # PS4,
# # WindowsStoreApps,
# # Switch,
# # N3DS,
# # tvOS.

BUILD_TARGETS = {
    "ios": "iOS"
    , "android": "Android"
    , "h5": "WebGL"
}


def call_unity_func(build_target, func_name, quit, log_name, channel=None
                    , channelId=None, out_path=None, version_name=None
                    , build_number=None, patch_file=None, debug=False, product=False,gitcommit=None):
    buf = [
        UNITY_PATH
        , "-batchmode"
        , "-nographics"
        , "-projectPath"
        , os.path.relpath(PROTJECT_PATH, WORK_PATH)
        , "-buildTarget"
        , build_target
    ]
    if quit:
        buf.append("-quit")
    if func_name is not None:
        buf.append("-executeMethod")
        buf.append(func_name)
    if log_name is not None:
        buf.append("-logFile")
        buf.append(log_name)
        LogFiles.append(log_name)
    if channel is not None:
        buf.append("-channel")
        buf.append(channel)
    if channelId is not None:
        buf.append("-channelId")
        buf.append(channelId)
    if out_path is not None:
        buf.append("-path")
        buf.append(out_path)
    if version_name is not None:
        buf.append("-versionname")
        buf.append(version_name)
    if build_number is not None:
        buf.append("-build")
        buf.append(build_number)
        buf.append("-versioncode")
        buf.append(build_number)
    if patch_file is not None:
        buf.append("-patchfile")
        buf.append(patch_file)
    if gitcommit is not None:
        buf.append("-gitcommit")
        buf.append(gitcommit)
    if debug:
        buf.append("-debug")
    if product:
        buf.append("-product")

    cmd = " ".join(buf)
    return execall(cmd)

def build_unity(platform, log_path, **kwargs):
    build_target = BUILD_TARGETS.get(platform.lower(), None)
    if build_target is None:
        return FAILURE("暂不支持该平台(%s)导出" % platform)
    if call_unity_func(build_target
            , "PluginLit.Core.Editor.BuildHelper.PreBuild"
            , True
            , os.path.join(log_path, "prebuildLog") if log_path else None
            , **kwargs):
        return FAILURE("Unity prebuild fail!", printLog=True)
    if call_unity_func(build_target
            , "PluginLit.Core.Editor.BuildHelper.Build"
            , False
            , os.path.join(log_path, "buildLog") if log_path else None
            , **kwargs):
        return FAILURE("Unity build fail!", printLog=True)


def generateAab(android_project_path, debug):
    gradlew = "./gradlew"
    if is_win_platform():
        gradlew = '"./gradlew.bat"'
    cmd = [
        gradlew,
        "bundleDebug" if debug else "bundleRelease"
    ]
    execall('cd "%s" && %s' % (android_project_path, " ".join(cmd)))
    mode = "debug" if debug else "release"
    aab_name = "launcher-%s.aab" % mode
    build_aab_file = os.path.join(android_project_path, "launcher", "build", "outputs", "bundle", mode, aab_name)
    return build_aab_file
    

def generateApk(android_project_path, debug):
    gradlew = "./gradlew"
    if is_win_platform():
        gradlew = '"./gradlew.bat"'
    cmd = [
        gradlew,
        "assembleDebug" if debug else "assembleRelease"
    ]
    execall('cd "%s" && %s' % (android_project_path, " ".join(cmd)))
    mode = "debug" if debug else "release"
    apk_name = "launcher-%s.apk" % mode
    build_apk_file = os.path.join(android_project_path, "launcher", "build", "outputs", "apk", mode, apk_name)
    return build_apk_file

def podInstall(project_path:str)->str:
    pod_file = os.path.join(project_path, "Podfile")
    if not os.path.exists(pod_file):
        return os.path.join(project_path, "Unity-iPhone.xcodeproj")

    execall("cd %s && pod install --repo-update" % project_path)
    return os.path.join(project_path, "Unity-iPhone.xcworkspace")


def generateIpa(ios_project_path:str, version_name:str, build_number:str, debug:bool, buildType="adHoc"):
    project = podInstall(ios_project_path)
    configuration = "debug" if debug else "release"
    build_config = BuildConfig()
    build_config.set_config(project, buildType, configuration, version_name, build_number, build_number)
    build_tool = BuildToolIOS(build_config, True, False)
    archive_path = build_tool.ios_archive()

    archive_info = ArchiveInfo()
    archive_info.from_archive_path(archive_path)

    export_path = build_config.get_export_path()
    ipa_path = build_tool.ios_archive_export(archive_path, archive_info, export_path, build_config.get_method())
    return ipa_path


def export_project(platform, channel, channelId, version_name, build_number, temp_path
    , debug, cache_log, product, gitcommit):
    check_path(temp_path)
    platform = platform.lower()
    try:
        build_unity(platform, temp_path if cache_log else None, version_name=version_name
                    , build_number=build_number, out_path=temp_path
                    , channel=channel, debug=debug, product=product, channelId=channelId,gitcommit=gitcommit)
    except Exit as e:
        return FAILURE(e.message)
    except Exception as err:
        return FAILURE(err)

def get_patch_tag(platform, version_name, root):
    return "%s_%s_%s" % (root, platform, version_name)


@task(help={
    "platform": "编译目标平台，目前支持ios、android和h5",
    "version_name": "版本号",
    "commit_id": "TAG对应的commit sha值",
    "root": "上传至的根目录(环境标识)",
})
def addPatchTag(context, platform, version_name, commit_id, root):
    repo = git.Repo(PROTJECT_PATH)
    try:
        repo.tree(commit_id)
    except Exception as err:
        return FAILURE("Cannot find commit with commit_id " + commit_id)
    tag_name = get_patch_tag(platform, version_name, root)
    for tag in repo.tags:
        if tag.name == tag_name:
            repo.delete_tag(tag)
    repo.create_tag(tag_name, commit_id)


def get_build_result(build_path):
    result_json = os.path.join(build_path, "buildResult.json")
    return parseJsonFile(result_json)


def replace_string(str, **kwargs):
    for key, value in kwargs.items():
        str = str.replace("{{%s}}" % key, value)
    return str


def build_single_aab(aab_path, apk_name_template, channel, channelId, version_name, build_number, temp_path, debug, cache_log, product, gitcommit):
    dump_now("start build all apks")
    export_project("android", channel, channelId, version_name, build_number, temp_path, debug, cache_log, product, gitcommit)
    dump_now("export android project")
    build_result = get_build_result(temp_path)
    android_project_path = build_result.get("projectPath", None)
    if android_project_path is None:
        print("buld_result >>>>>>> ", str(build_result))
        return FAILURE("Cannot get android project path")
    aab_file_name = generateAab(android_project_path, debug)
    dump_now("generate aab completed")
    if not os.path.exists(aab_file_name):
        return FAILURE("找不到构建的安卓AAB" + aab_file_name)
    aab_name = replace_string(apk_name_template, platform="android", channel=channel, channelId=channelId, version_name=version_name, build_number=build_number)
    aab_name = aab_name.replace(".apk", ".aab")
    target_aab_file = os.path.join(aab_path, aab_name)
    check_path(aab_path)
    rm_file(target_aab_file)
    shutil.move(aab_file_name, target_aab_file)
    

def build_all_apks(apks_path, apk_name_template, channel, channelIds, version_name, build_number, temp_path
    , debug, cache_log, product, gitcommit):
    channelId_list = channelIds.split(',')
    channelId = channelId_list[0]
    dump_now("start build all apks")
    export_project("android", channel, channelId, version_name, build_number, temp_path, debug, cache_log, product, gitcommit)
    dump_now("export android project")
    build_result = get_build_result(temp_path)
    android_project_path = build_result.get("projectPath", None)
    if android_project_path is None:
        print("buld_result >>>>>>> ", str(build_result))
        return FAILURE("Cannot get android project path")
    apk_file_name = generateApk(android_project_path, debug)
    dump_now("generate first apk")
    if not os.path.exists(apk_file_name):
        return FAILURE("找不到构建的安卓APK" + apk_file_name)
    apk_name = replace_string(apk_name_template, platform="android", channel=channel, channelId=channelId, version_name=version_name, build_number=build_number)
    target_apk_file = os.path.join(apks_path, apk_name)
    check_path(apks_path)
    rm_file(target_apk_file)
    shutil.move(apk_file_name, target_apk_file)
    return build_result


def build_ios_installer(installer_path, channel, channelId, version_name, build_number, temp_path
    , debug, cache_log, product, gitcommit, iosBuildType="adHoc"):
    dump_now("start build ios installer")
    export_project("ios", channel, channelId, version_name, build_number, temp_path, debug, cache_log, product, gitcommit)
    dump_now("export ios project completed")
    build_result = get_build_result(temp_path)
    ios_project_path = build_result.get("projectPath", None)
    if ios_project_path is None:
        return FAILURE("Cannot get ios project path")
    ipa_file_name = generateIpa(ios_project_path, version_name, build_number, debug, iosBuildType)
    dump_now("generated ipa")
    if not os.path.exists(ipa_file_name):
        return FAILURE("找不到构建的IPA:" + ipa_file_name)
    copy_file(ipa_file_name, os.path.join(installer_path, "app.ipa"))
    return build_result


def build_h5_web(channel, channelId, version_name, build_number, temp_path
                        , debug, cache_log, product, gitcommit):
    dump_now("start build h5 web")
    export_project("h5", channel, channelId, version_name, build_number, temp_path, debug, cache_log, product, gitcommit)
    dump_now("export h5 project completed")
    build_result = get_build_result(temp_path)
    h5_project_path = build_result.get("projectPath", None)
    if h5_project_path is None:
        return FAILURE("Cannot get h5 project path")
    return build_result


def upload_bugly_symbols(build_result:dict):
    if build_result is None:
        return
    bundleId = build_result.get("bundleId", None)
    if bundleId is None:
        return
    platform = build_result.get("platform", None)
    if platform is None:
        return
    buglyId = build_result.get("buglyId", None)
    if buglyId is None:
        return
    buglyKey = build_result.get("buglyKey", None)
    if buglyKey is None:
        return
    buglyVersion = build_result.get("buglyVersion", None)
    if buglyVersion is None:
        return
    buglySymbols = build_result.get("buglySymbols", None)
    if buglySymbols is None:
        return
    if not os.path.exists(buglySymbols):
        return
    projectPath = build_result.get("projectPath", None)
    if projectPath is None:
        return
    if not os.path.exists(projectPath):
        return
    tool_jar = os.path.join(projectPath, "buglyqq-upload-symbol.jar")
    if not os.path.exists(tool_jar):
        return
    cmds = ["java",
        "-jar", tool_jar,
        "-appid", buglyId,
        "-appkey", buglyKey,
        "-inputSymbol", buglySymbols,
        "-version", buglyVersion,
        "-bundleid", bundleId,
        "-platform", platform.title()
        ]
    execall(" ".join(cmds))


# -----------------
@task(help={
    "platform": "编译目标平台，目前支持ios、android和h5",
    "channel": "目标渠道",
    "channelIds": "目标渠道ID，多渠道ID以逗号','隔开",
    "version_name": "版本号名称",
    "build_number": "build号",
    "out_path": "输出目录",

    "debug": "DEBUG模式会打开构建时的开发模式选项，且增加DEBUG宏",
    "log": "保存log文件",
    "product": "是否为生产模式",

    'apk_name_template' : "APK目标文件名称模版",
    'gitcommit': "gitcommit号，用来标识资源版本TAG",
    'buildBundle': "是否构建bundle文件，无该选项时默认构建APK",
})
def buildAppsFlow(context, platform, channel, channelIds, version_name, build_number, out_path
    , apk_name_template=None, debug=False, log=True, product=False, gitcommit=None, iosBuildType="adHoc", buildBundle=False):
    temp_path = os.path.join(PROTJECT_PATH, "Build", platform, "build_%s" % build_number)
    rm_dir(temp_path)
    try:
        build_result = None
        if platform == 'ios':
            installer_path = os.path.join(temp_path, "installer")
            build_result = build_ios_installer(installer_path, channel, channelIds, version_name, build_number, temp_path, debug, log, product, gitcommit, iosBuildType)
        elif platform == "android":
            if buildBundle:
                aab_path = os.path.join(temp_path, "aab")
                build_result = build_single_aab(aab_path, apk_name_template, channel, channelIds, version_name, build_number, temp_path
                                              , debug, log, product, gitcommit)
            else:
                apks_path = os.path.join(temp_path, "apks")
                build_result = build_all_apks(apks_path, apk_name_template, channel, channelIds, version_name, build_number, temp_path
                  , debug, log, product, gitcommit)
        elif platform == "h5":
            build_result = build_h5_web(channel, channelIds, version_name, build_number, temp_path, debug, log, product, gitcommit)
        else:
            raise Exception("not support platform " + platform)
#         upload_bugly_symbols(build_result)
    except Exit as exit:
        shutil.move(temp_path, out_path)
        return FAILURE(exit.message)
    except Exception as err:
        return FAILURE("Build Fail with error::" + str(err))
        
    shutil.move(temp_path, out_path)
    return SUCESS("Build Completed")
