#include "stdafx.h"
#include "camera.h"
#include "xml-load.h"
#include "renderer.h"
#include "../GeometryLib/vector3d.h"
#include "../GeometryLib/transform3d.h"

Camera::Camera(): SceneActor3D()
{
}

Camera::~Camera()
{
}

Matrix44 Camera::getModelviewMatrix() const
{
	Matrix44 mat, rot, trans;
	rot.setRotation(m_transform.rotation().inverse());
	trans.setTranslation(m_transform.translation().inverse());
	mat = rot*trans;

	return mat;
}

Camera* Camera::getInstance(tinyxml2::XMLElement* pNode)
{
	tinyxml2::XMLElement* pChild = pNode->FirstChildElement();
	if (!strcmp(pChild->Name(), XML_TAG_SIMPLE_CAMERA))
		return new SimpleCamera(pChild);
	return nullptr;
}

SimpleCamera::SimpleCamera(tinyxml2::XMLElement* pNode)
{
	tinyxml2::XMLElement* pChild = pNode->FirstChildElement(XML_TAG_TRANSFORM);
	if (pChild)
		XML::load(pChild,m_transform);
}

SimpleCamera::SimpleCamera()
{
}

void SimpleCamera::set()
{
	//set projection matrix
	glMatrixMode(GL_PROJECTION);
	Matrix44 perspective;
	//the near plane has width 1 and the height is adjusted according to the aspect ratio
	int screenHeight, screenWidth;
	Renderer::get()->getWindowsSize(screenWidth, screenHeight);
	double aspectRatio = (double)screenWidth / (double)screenHeight;
	perspective.setPerspective(0.5, 0.5/aspectRatio, nearPlane, farPlane);
	glLoadMatrixd(perspective.asArray());

	//set modelview matrix
	glMatrixMode(GL_MODELVIEW);
	Matrix44 matrix = getModelviewMatrix();
	glLoadMatrixd(matrix.asArray());

	m_frustum.fromCameraMatrix(matrix*perspective);
}

//This method sets an orthogonal view that maps the screen to coordinates [0.0,0.0] - [1.0,1.0]
void Camera::set2DView()
{
	//set projection matrix
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	gluOrtho2D(0.0, 1.0, 0.0, 1.0);

	//set modelview matrix
	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();
}