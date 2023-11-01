using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ModelController : MonoBehaviour
{
    [Header("Debug")]
    public float eyeBallRadius = 0.012F;
    public float landmarkPreviewScale = 1F / 640F;
    public GameObject landmarkPositionPreviewPrefab;
    public LineRenderer landmarkNormalPreviewPrefab;
    public Material eyeCentreMaterial, eyeFrameMaterial;
    public Transform faceMeshPreviewCentre;

    [Header("Averaging")]
    float _averagingSampleAmount = 3;

    [Header("Head rotation mode")]
    [SerializeField]
    bool invertX;
    [SerializeField]
    bool lookWorldRaycastHit = true;
    [SerializeField]
    new Camera camera;

    [Header("Iris distance measurement")]
    [SerializeField]
    float _focalLength;
    [SerializeField]
    float _irisDiameter = 0.0117F;
    [SerializeField]
    Text _irisDistanceText;
    [SerializeField]
    Text _irisPosText;
    [SerializeField]
    Text _irisLookAtText;
    [SerializeField]
    Image _irisLookAtImage;
    [SerializeField]
    Image _irisLookAtImage2;
    [SerializeField]
    Image _noseLookAtImage;
    [SerializeField]
    Image _eyeGazePredictionImage;
    [SerializeField]
    GameObject _screenPreview;
    [SerializeField]
    GameObject _screenLookAtPreview;

    [Header("Model")]
    [SerializeField]
    TrackModel _trackModel;

    [Header("Hand")]
    [SerializeField]
    Transform _leftHandIKGoal;
    [SerializeField]
    Transform _rightHandIKGoal;

    [Header("Eyes")]
    [SerializeField]
    float eyeOpenDistance_ratioAgainstUnit = 0.5F;
    [SerializeField]
    float eyeCloseDistance_ratioAgainstUnit = 0.01F;
    [SerializeField]
    float eyeOpenBlendShapeRatio = 0.0f;
    [SerializeField]
    float eyeCloseBlendShapeRatio = 85.0f;

    [Header("Mouth")]
    [SerializeField]
    float mouthOpenDistance_ratioAgainstUnit = 1F;
    [SerializeField]
    float mouthCloseDistance_ratioAgainstUnit = 1F;
    [SerializeField]
    float mouthOpenBlendShapeRatio = 0.0f;
    [SerializeField]
    float mouthCloseBlendShapeRatio = 1;

    [Header("Reference")]
    [SerializeField]
    Client _client;

    List<List<Vector3>> _averagingLandmarkSamples = new List<List<Vector3>>();
    List<GameObject> positionPreviewObjects = new List<GameObject>();
    LineRenderer leftEyeNormalPreview;
    LineRenderer rightEyeNormalPreview;
    GameObject leftEyeCentrePreview;
    GameObject rightEyeCentrePreview;
    LineRenderer faceNormalPreview;

    Transform neckTransform;
    Quaternion neckRotationOrigin;
    Vector3 rightIrisNoseLookAtCameraSpace;
    Vector3 ScreenCentreCameraSpace => enableNeckAngleFix ? screenCentreCameraSpaceBackup : Vector3.zero;
    Vector3 screenCentreCameraSpaceBackup;
    Vector3 noseSpaceLeftEyeCentreMeter;
    Vector3 noseSpaceRightEyeCentreMeter;
    float meterPerPixel;
    Vector3 leftIrisPosCameraSpace;
    Vector3 rightIrisPosCameraSpace;
    Vector3 leftIrisLandmark, rightIrisLandmark, noseTipLandmark;
    Vector3 faceNormal;
    Vector3 leftEyeFixedReferenceLandmark, rightEyeFixedReferenceLandmark;
    Vector3 leftEyePosFromFixedReference, rightEyePosFromFixedReference;

    public float IrisDistance { get; private set; }
    public Vector2 NoseLookAtScreenPos { get; private set; }
    public bool enableNeckAngleFix = true;
    private void Start()
    {
        for (int i = 0; i < 478; i++)
        {
            GameObject obj = Instantiate(landmarkPositionPreviewPrefab, faceMeshPreviewCentre);
            obj.name = "landmark" + i;
            positionPreviewObjects.Add(obj);
        }
        leftEyeNormalPreview = Instantiate(landmarkNormalPreviewPrefab, faceMeshPreviewCentre);
        leftEyeNormalPreview.name = "leftEyeNormalPreview";
        rightEyeNormalPreview = Instantiate(landmarkNormalPreviewPrefab, faceMeshPreviewCentre);
        rightEyeNormalPreview.name = "rightEyeNormalPreview";
        faceNormalPreview = Instantiate(landmarkNormalPreviewPrefab, faceMeshPreviewCentre);
        faceNormalPreview.name = "faceNormalPreview";

        leftEyeCentrePreview = Instantiate(landmarkPositionPreviewPrefab, faceMeshPreviewCentre);
        leftEyeCentrePreview.GetComponent<Renderer>().material = eyeCentreMaterial;
        rightEyeCentrePreview = Instantiate(landmarkPositionPreviewPrefab, faceMeshPreviewCentre);
        rightEyeCentrePreview.GetComponent<Renderer>().material = eyeCentreMaterial;

        SetPreviewObjectsMaterial(eyeFrameMaterial, 374, 386, 263, 362, 145, 159, 173, 33);

        OnModelChange();
    }
    private void OnModelChange()
    {
        neckTransform = _trackModel.Animator.GetBoneTransform(HumanBodyBones.Neck);
        neckRotationOrigin = neckTransform.localRotation;
        _trackModel.AddIKPassAction(_ =>
        {
            _trackModel.Animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandIKGoal.position);
            _trackModel.Animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandIKGoal.rotation);
            _trackModel.Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            _trackModel.Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            _trackModel.Animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandIKGoal.position);
            _trackModel.Animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandIKGoal.rotation);
            _trackModel.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            _trackModel.Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            print("ik pass");
        });
    }
    public Transform RecursiveFind(Transform root, string name)
    {
        Transform child = root.Find(name);
        if(child == null)
        {
            foreach(Transform eachChild in root)
            {
                child = RecursiveFind(eachChild, name);
                if (child != null)
                    break;
            }
        }
        return child;
    }
    public void UpdateHandModel(List<Vector3> landmarks, bool handedness)
    {
        if (landmarks.Count == 0)
            return;
        Vector3 handRootPos = neckTransform.position + landmarks[0];
        Quaternion handRootRot = Quaternion.identity;
        if (handedness) //right hand
        {
            _rightHandIKGoal.transform.position = handRootPos;
        }
        else //left hand
        {
            _leftHandIKGoal.transform.position = handRootPos;
        }
    }
    public void UpdateHeadModel(List<Vector3> landmarks, Vector2 captureSize, Vector2 gazePointPrediction)
    {
        if (landmarks.Count == 0)
            return;
        if (_averagingLandmarkSamples.Count < _averagingSampleAmount)
        {
            _averagingLandmarkSamples.Add(landmarks);
        }
        else
        {
            _averagingLandmarkSamples.RemoveFirst();
            _averagingLandmarkSamples.Add(landmarks);
            for (int landMarkIndex = 0; landMarkIndex < landmarks.Count; landMarkIndex++)
            {
                Vector3 positionSum = Vector3.zero;
                foreach (List<Vector3> positionList in _averagingLandmarkSamples)
                {
                    positionSum += positionList[landMarkIndex];
                }
                landmarks[landMarkIndex] = positionSum / _averagingSampleAmount;
            }
        }
        for (int i = 0; i < landmarks.Count; i++)
        {
            positionPreviewObjects[i].transform.localPosition = landmarks[i] * landmarkPreviewScale;
        }
        leftIrisLandmark = landmarks[468 + 5];
        rightIrisLandmark = landmarks[468 + 0];

        //Vector3 noseNormal = Vector3.Cross(noseVec1, noseVec2).normalized;
        noseTipLandmark = landmarks[4];
        faceNormal = (landmarks[4] - (landmarks[220] + landmarks[440]) / 2).normalized;
        Vector3 faceUp = Vector3.Cross(landmarks[440] - landmarks[4], landmarks[220] - landmarks[4]);
        Quaternion faceRotation = Quaternion.LookRotation(faceNormal, faceUp);

        float unitDistance = Vector3.Distance(landmarks[220], landmarks[440]);

        float eyeOpenDistance = unitDistance * eyeOpenDistance_ratioAgainstUnit;
        float eyeCloseDistance = unitDistance * eyeCloseDistance_ratioAgainstUnit;
        float leftEyeBlendShapeRatio = (Vector3.Distance(landmarks[374], landmarks[386]) - eyeCloseDistance) / (eyeOpenDistance - eyeCloseDistance);
        float rightEyeBlendShapeRatio = (Vector3.Distance(landmarks[145], landmarks[159]) - eyeCloseDistance) / (eyeOpenDistance - eyeCloseDistance);
        float eyeBlendShapeRatio = Mathf.Clamp(Mathf.Lerp(eyeCloseBlendShapeRatio, eyeOpenBlendShapeRatio, leftEyeBlendShapeRatio), 0, 100);
        _trackModel.SetEyeOpenRatio(eyeBlendShapeRatio);

        float mouthOpenDistance = unitDistance * mouthOpenDistance_ratioAgainstUnit;
        float mouthCloseDistance = unitDistance * mouthCloseDistance_ratioAgainstUnit;
        float mouthBlendShapeRatio = (Vector3.Distance(landmarks[13], landmarks[14]) - mouthCloseDistance) / (mouthOpenDistance - mouthCloseDistance);
        mouthBlendShapeRatio = Mathf.Clamp(Mathf.Lerp(mouthCloseBlendShapeRatio, mouthOpenBlendShapeRatio, mouthBlendShapeRatio), 0, 100);
        _trackModel.SetMouthOpenRatio(mouthBlendShapeRatio);

        //iris distance measurement
        float rightIrisDiameterPixel = Vector3.Distance(landmarks[468 + 1], landmarks[468 + 3]);
        meterPerPixel = _irisDiameter / rightIrisDiameterPixel;
        IrisDistance = _focalLength * meterPerPixel;
        _irisDistanceText.text = IrisDistance * 100 + "cm";
        leftIrisPosCameraSpace.x = (leftIrisLandmark.x > 0 ? 1 : -1) * Mathf.Sqrt(IrisDistance * IrisDistance / (_focalLength * _focalLength / leftIrisLandmark.x / leftIrisLandmark.x + 1));
        leftIrisPosCameraSpace.y = (leftIrisLandmark.y > 0 ? 1 : -1) * Mathf.Sqrt(IrisDistance * IrisDistance / (_focalLength * _focalLength / leftIrisLandmark.y / leftIrisLandmark.y + 1));
        leftIrisPosCameraSpace.z = - leftIrisPosCameraSpace.x * _focalLength / leftIrisLandmark.x;
        rightIrisPosCameraSpace.x = (rightIrisLandmark.x > 0 ? 1 : -1) * Mathf.Sqrt(IrisDistance * IrisDistance / (_focalLength * _focalLength / rightIrisLandmark.x / rightIrisLandmark.x + 1));
        rightIrisPosCameraSpace.y = (rightIrisLandmark.y > 0 ? 1 : -1) * Mathf.Sqrt(IrisDistance * IrisDistance / (_focalLength * _focalLength / rightIrisLandmark.y / rightIrisLandmark.y + 1));
        rightIrisPosCameraSpace.z = -rightIrisPosCameraSpace.x * _focalLength / rightIrisLandmark.x;
        _irisPosText.text = "dx: " + rightIrisPosCameraSpace.x * 1000 + "mm, dy: " + rightIrisPosCameraSpace.y * 1000 + "mm, dz: " + rightIrisPosCameraSpace.z * -1000 + "mm";
        Ray rightIrisNoseRay = new Ray(rightIrisPosCameraSpace, faceNormal);
        if(new Plane(Vector3.forward, Vector3.zero).Raycast(rightIrisNoseRay, out float rightIrisNoseRayHitDistance))
        {
            rightIrisNoseLookAtCameraSpace = rightIrisNoseRay.GetPoint(rightIrisNoseRayHitDistance);
            _irisLookAtText.text = rightIrisNoseLookAtCameraSpace * 1000 + "(mm)";
            Vector2 noseLookAtScreenPos = (rightIrisNoseLookAtCameraSpace - ScreenCentreCameraSpace) * (Screen.dpi / 0.0254F);
            noseLookAtScreenPos.x *= -1;
            NoseLookAtScreenPos = new Vector2(Screen.width, Screen.height) / 2 + noseLookAtScreenPos;
            _noseLookAtImage.transform.position = NoseLookAtScreenPos;
        }
        _eyeGazePredictionImage.transform.position = gazePointPrediction;

        if (lookWorldRaycastHit)
        {
            bool found = false;
            RaycastHit closestHitInfo = new();
            closestHitInfo.distance = float.MaxValue;
            Ray gazePosScreenRay = camera.ScreenPointToRay(_noseLookAtImage.transform.position);
            foreach (var hitInfo in Physics.RaycastAll(gazePosScreenRay))
            {
                if(hitInfo.distance < closestHitInfo.distance && hitInfo.collider.GetComponentInParent<ModelController>() == null)
                {
                    closestHitInfo = hitInfo;
                    found = true;
                }
            }
            if(found)
            {
                neckTransform.LookAt(closestHitInfo.point);
            }
        }
        else
        {
            neckTransform.localRotation = neckRotationOrigin;
            Vector3 faceRotationEular = faceRotation.eulerAngles + Quaternion.FromToRotation((ScreenCentreCameraSpace - rightIrisPosCameraSpace).normalized, (-rightIrisPosCameraSpace).normalized).eulerAngles;
            if (invertX)
            {
                faceRotationEular.y *= -1;
                faceRotationEular.z *= -1;
            }
            //faceRotationEular = _modelRoot.TransformVector(faceRotationEular);
            //Quaternion resetRotation = Quaternion.Inverse(_modelRoot.rotation) * neckTransform.rotation;
            //neckTransform.rotation = _modelRoot.rotation;
            //neckTransform.localEulerAngles += faceRotationEular;
            //neckTransform.rotation *= resetRotation;
            //neckTransform.rotation = Quaternion.AngleAxis(faceRotationEular.z, _modelRoot.forward) * Quaternion.AngleAxis(faceRotationEular.x, _modelRoot.right) * Quaternion.AngleAxis(faceRotationEular.y, _modelRoot.up);
            
            neckTransform.rotation *= Quaternion.Inverse(neckTransform.rotation) * Quaternion.AngleAxis(faceRotationEular.z, _trackModel.transform.forward) * Quaternion.AngleAxis(faceRotationEular.x, _trackModel.transform.right) * Quaternion.AngleAxis(faceRotationEular.y, _trackModel.transform.up) * neckTransform.rotation;
            //neckTransform.rotation *= Quaternion.Inverse(neckTransform.rotation) * neckTransform.rotation;
            //neckTransform.rotation *= Quaternion.Inverse(neckTransform.rotation) * neckTransform.rotation;
        }
        //new iris look at pos estimation
        leftEyeFixedReferenceLandmark = (landmarks[263] + landmarks[362]) / 2;
        rightEyeFixedReferenceLandmark = (landmarks[173] + landmarks[33]) / 2;
        Vector3 leftEyeCentreLandmark = leftEyeFixedReferenceLandmark + leftEyePosFromFixedReference / meterPerPixel;
        Vector3 rightEyeCentreLandmark = rightEyeFixedReferenceLandmark + rightEyePosFromFixedReference / meterPerPixel;
        leftEyeCentrePreview.transform.localPosition = leftEyeCentreLandmark * landmarkPreviewScale;
        rightEyeCentrePreview.transform.localPosition = rightEyeCentreLandmark * landmarkPreviewScale;
        leftEyeNormalPreview.SetPositions(new Vector3[] { leftEyeCentrePreview.transform.localPosition, leftEyeCentrePreview.transform.localPosition + (leftIrisLandmark - leftEyeCentreLandmark).normalized * 32 });
        rightEyeNormalPreview.SetPositions(new Vector3[] { rightEyeCentrePreview.transform.localPosition, rightEyeCentrePreview.transform.localPosition + (rightIrisLandmark - rightEyeCentreLandmark).normalized * 32 });

        faceNormalPreview.SetPositions(new Vector3[] { landmarks[4] * landmarkPreviewScale, landmarks[4] * landmarkPreviewScale + faceNormal * 32 });
        
        Ray rightIrisRay = new Ray(rightIrisPosCameraSpace, rightIrisLandmark - rightEyeCentreLandmark);
        Ray leftIrisRay = new Ray(leftIrisPosCameraSpace, leftIrisLandmark - leftEyeCentreLandmark);
        if (new Plane(Vector3.forward, Vector3.zero).Raycast(rightIrisRay, out float rightIrisRayHitDistance)
            && new Plane(Vector3.forward, Vector3.zero).Raycast(leftIrisRay, out float leftIrisRayHitDistance))
        {
            Vector3 rightIrisLookAtCameraSpace = rightIrisRay.GetPoint(rightIrisRayHitDistance);
            Vector3 leftIrisLookAtCameraSpace = leftIrisRay.GetPoint(leftIrisRayHitDistance);
            //_irisLookAtText.text = rightIrisLookAtCameraSpace * 1000 + "(mm)";
            Vector2 screenLookAtPos = (rightIrisLookAtCameraSpace - ScreenCentreCameraSpace) * (Screen.dpi / 0.0254F);
            screenLookAtPos.x *= -1;
            _irisLookAtImage.transform.position = new Vector2(Screen.width, Screen.height) / 2 + screenLookAtPos;
            screenLookAtPos = (leftIrisLookAtCameraSpace - ScreenCentreCameraSpace) * (Screen.dpi / 0.0254F);
            screenLookAtPos.x *= -1;
            _irisLookAtImage2.transform.position = new Vector2(Screen.width, Screen.height) / 2 + screenLookAtPos;
        }

        _screenPreview.transform.localPosition = (rightIrisLandmark + (-rightIrisPosCameraSpace + ScreenCentreCameraSpace) / meterPerPixel) * landmarkPreviewScale;
        _screenPreview.transform.localScale = _screenPreview.transform.localScale.Set(x: Screen.width * (0.0254F / Screen.dpi) / meterPerPixel * landmarkPreviewScale, y: Screen.height * (0.0254F / Screen.dpi) / meterPerPixel * landmarkPreviewScale);


        Ray irisNoseRay = new Ray(rightEyeCentrePreview.transform.position, faceNormal);
        if (new Plane(_screenPreview.transform.forward, _screenPreview.transform.position).Raycast(irisNoseRay, out float hitdistance))
        {
            _screenLookAtPreview.transform.position = irisNoseRay.GetPoint(hitdistance);
        }
        Ray testPreviewRightIrisRay = new Ray(rightEyeCentrePreview.transform.position, rightIrisLandmark - rightEyeCentreLandmark);
        if(new Plane(_screenPreview.transform.forward, _screenPreview.transform.position).Raycast(testPreviewRightIrisRay, out float previewRightIrisRayHitDistance)) {
            //_screenLookAtPreview.transform.position = testPreviewRightIrisRay.GetPoint(previewRightIrisRayHitDistance);
            Vector2 raycastGuess = _screenPreview.transform.InverseTransformPoint(_screenLookAtPreview.transform.position);
            raycastGuess.x = (0.5F - raycastGuess.x) * Screen.width;
            raycastGuess.y = (0.5F + raycastGuess.y) * Screen.height;
            //print("ratio: " + (raycastGuess.x / _screenPreview.transform.localScale.x) + ", " + (raycastGuess.y / _screenPreview.transform.localScale.y));
            _irisLookAtImage.transform.position = raycastGuess;
        }

        //model train
        _client.AppendForPredicationPoints( new Vector3[] {
            landmarks[374], landmarks[386], landmarks[263], landmarks[362], landmarks[473],
            landmarks[145], landmarks[159], landmarks[173], landmarks[33], landmarks[468] }, Input.mousePosition);
    }
    private void SetPreviewObjectsMaterial(Material material, params int[] landmarkIDs)
    {
        foreach(int landmarkID in landmarkIDs)
        {
            positionPreviewObjects[landmarkID].GetComponent<Renderer>().material = material;
        }
    }
    public void SetScreenCentreZero()
    {
        screenCentreCameraSpaceBackup = rightIrisNoseLookAtCameraSpace;
    }
    public void SetEyeBallCentreZero()
    {
        Vector3 leftIrisLookDirection = -leftIrisPosCameraSpace.normalized;
        Vector3 rightIrisLookDirection = -rightIrisPosCameraSpace.normalized;
        Vector3 leftEyeBallCentreCameraSpace = leftIrisPosCameraSpace - leftIrisLookDirection * eyeBallRadius;
        Vector3 rightEyeBallCentreCameraSpace = rightIrisPosCameraSpace - rightIrisLookDirection * eyeBallRadius;
        Vector3 leftEyeFixedReferencePos = leftIrisPosCameraSpace + (leftEyeFixedReferenceLandmark - leftIrisLandmark) * meterPerPixel;
        Vector3 rightEyeFixedReferencePos = rightIrisPosCameraSpace + (rightEyeFixedReferenceLandmark - rightIrisLandmark) * meterPerPixel;
        leftEyePosFromFixedReference = leftEyeBallCentreCameraSpace - leftEyeFixedReferencePos;
        rightEyePosFromFixedReference = rightEyeBallCentreCameraSpace - rightEyeFixedReferencePos;
    }
    public void SetAvater(TrackModel model)
    {
        foreach(Transform modelTf in _trackModel.transform)
        {
            modelTf.gameObject.SetActive(false);
        }
        model.gameObject.SetActive(true);
        _trackModel = model;
        OnModelChange();
    }
}
