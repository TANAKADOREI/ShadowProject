# ShadowProject

깃허브 비공개 개인 리포지토리에서 공개하고 싶은 부분만 공개하고 싶어서 만든 프로그램



* 프로그램 인수 사용가능. 별칭을 인수로 보내면 바로 가능.

* 기본적으로 JSON문법과 PrelRegex문법을 알아야 합니다.



## Menu

### ShowList

동기화가될 등록된 디렉터리 목록을 보여줍니다

### Register

디렉토리를 동기화 목록에 등록합니다

### SyncAll

등록된 동기화 목록을 모두 실행합니다

### Sync

동기화 합니다

### DeleteShadow

동기화 목록에서 제거합니다

### DeleteAllShadow

동기화 목록을 비웁니다

### Exit

프로그램 종료

# \__SDWP_PROFILE__

동기화 디렉터리랑 같은 부모 디렉터리에 위치하는 캐시 디렉터리

원본 디렉터리 위치가 C://DIR라면 C://\__SDWP_PROFILE__위치에 생성되는 디렉터리

## Config.json

```
{
  "ThreadCount": 4,//병렬작업 스레드 수(사용되지 않음)
  "StringBuilderPoolCapacity": 16,
  "BufferSize": 4096,//파일 IO버퍼
  "BufferPoolCapacity": 16
}
```



## Manifest.json

* 배열 데이터는 추가가능
* 비주얼 스튜디오2019의 CMAKE 필터링및 동기화 예시
* Selection.Enable이 비활성화 할경우 작업이 없음

### 접두사

#### [알파벳]__

알파벳 순서대로 작동됨

#### Priority__

검사 우선 순위

#### Use__

사용 여부

#### N__

값 반전 true일때 동기화 타겟이 됩니다

#### Regex__

정규식 패턴

### 옵션

#### Enable

사용여부

#### ChooseOneOf_AND_or_OR

로직의 비교 연산자. AND는 모든 값이 true이어야 동기화 타겟이되고, OR는 하나라도 true라면 동기화 대상이 됨

```
{
//명시적 매니 페스트 버전 확인용
  "MANIFEST_VERSION": 4,
  //소스 디렉터리 경로
  "SourceDirectory": "",
  //도착 디렉터리 경로
  "DestDirectory": "",
  //파일 복사 방향
  "FromSourceToDest": true,
  //타겟팅 방식
  "Selection": {
  //첫번째 로직, 디렉터리 경로 검사
    "DirectorySelectionRegex": {
    	//상대경로 기준, "C:\\CMAKE_DIR"에서 "CMAKE_DIR"위치에서 하위로 검사 "C://"는 포함하지 않음
		"Priority__RelativePathDirNameRegex": 0,
		"Use__UseRelativePathDirNameRegex": false,
		"N__RelativePathDirNameRegex": false,
		"Regex__RelativePathDirNameRegex": "",
    	//부모 폴더 이름만 검사 "C:\\CMAKE_DIR\A"에서 "CMAKE_DIR", "A"이런식으로 경로를 포함하지 않는 이름만
		"Priority__DirNameRegex": 0,
		"Use__UseDirNameRegex": false,
		"N__DirNameRegex": false,
		"Regex__DirNameRegex": "",
    	//절대경로 기준, "C:\\CMAKE_DIR"에서 "CMAKE_DIR"위치에서 하위로 검사 "C://"는 포함
		"Priority__DirPathRegex": 0,
		"Use__UseDirPathRegex": true,
		"N__DirPathRegex": true,
		"Regex__DirPathRegex": "((build)|(out))",
		"ChooseOneOf_AND_or_OR": "AND"
    },
    //두번째 로직, 파일 경로 검사
    "FileSelectionRegex": {
    //파일이름만 "File.file"중 File
		"Priority__FileNameRegex": 0,
		"Use__FileNameRegex": false,
		"N__FileNameRegex": false,
		"Regex__FileNameRegex": "",
    //파일 확장자만 "File.file"중 file
		"Priority__ExtRegex": 0,
		"Use__ExtRegex": true,
		"N__ExtRegex": false,
		"Regex__ExtRegex": "^((txt)|(h)|(cpp)|(c))$",
    //파일이름 "File.file"중 File.file
		"Priority__FileFullNameRegex": 0,
		"Use__FileFullNameRegex": false,
		"N__FileFullNameRegex": false,
		"Regex__FileFullNameRegex": "",
    //파일전체 경로포함 "C://CMAKE_DIR/File.file"
		"Priority__FilePathRegex": 0,
		"Use__FilePathRegex": false,
		"N__FilePathRegex": false,
		"Regex__FilePathRegex": "",
      "ChooseOneOf_AND_or_OR": "AND"
    },
    //세번째 로직, 타겟팅된 파일들 중 변경된 파일만 비교후, 타겟팅
    "FileComparison": {
    //마지막으로 수정된 날짜
      "Priority__FileInfo__CompareLastModifiedDate": 0,
      "Use__FileInfo__CompareLastModifiedDate": false,
      "N__FileInfo__CompareLastModifiedDate": false,
      //마지막으로 액세스된 날짜
      "Priority__FileInfo__CompareLastAccessedDate": 0,
      "Use__FileInfo__CompareLastAccessedDate": false,
      "N__FileInfo__CompareLastAccessedDate": false,
      //생성된 날짜
      "Priority__FileInfo__CompareCreatedDate": 0,
      "Use__FileInfo__CompareCreatedDate": false,
      "N__FileInfo__CompareCreatedDate": false,
      //파일의 해쉬
      "Priority__FileInfo__CompareHash": 0,
      "Use__FileInfo__CompareHash": false,
      "N__FileInfo__CompareHash": false,
      //파일의 크기
      "Priority__FileInfo__CompareSize": 0,
      "Use__FileInfo__CompareSize": true,
      "N__FileInfo__CompareSize": false,
      "ChooseOneOf_AND_or_OR": "AND"
    },
    "Enable": true
  },
  //파일 교정기
  "FileProofreader": {
  //텍스트 파일
    "TextFile__IsNotDocFile": [
      {
      //확장자
        "A__Extensions": [
          "txt",
          "json"
        ],
        //정규화할 개행 문자 NEWLINE_CR(CR),NEWLINE_LF(LF),NEWLINE_CRLF(CRLF),NEWLINE_NONE(아무것도 안함),NEWLINE_ERASE(개행 문자 제거) 이중 하나 선택
        "B__PleaseSelectTheNewlineYouWant(NEWLINE_CR,NEWLINE_LF,NEWLINE_CRLF,NEWLINE_NONE,NEWLINE_ERASE)": "NEWLINE_LF",
        //C__는 현재 사용되지 않음
        "C__RemoveComment": false,
        "C__CommentRegex": {
          "SingleCommentSign": "",
          "MultiCommentOpenSign": "",
          "MultiCommentCloseSign": "",
          "Enable": false
        },
        //들여쓰기 정규화
        "D__IndentConverter": {
          "Space": 4,
          "Tab": 1,
          //스페이스를 탭으로 변환
          "ConvertSapceToTab": false,
          "Enable": false
        },
        //인코딩 변환
        "E__Encoding": {
          "EncodingName__IfEmptyDontConvertEncoding": "utf-8",
          "Enable": false
        },
        "Enable": false
      }
    ],
    "Enable": false
  },
  //동기화 프로세스
  "SyncProcessing": {
  //비대칭 디렉터리 제거
    "RemoveAsymmetricDirectories": true,
    //비대칭 파일 제거
    "RemoveAsymmetricFiles": true,
    //비어있는 디렉터리 제거
    "RemoveEmptyDirectories": true
  }
}
```

